using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgLitePanel.Core.Abstractions.Exceptions;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Core.Abstractions.TdLib;
using Client = WTelegram.Client;

namespace TgLitePanel.Infrastructure.WTelegram;

/// <summary>
/// WTelegram 客户端管理器，实现租约机制和自动清理
/// </summary>
public sealed class WTelegramClientManager : ITdClientManager
{
    private sealed class ClientEntry
    {
        public required long AccountId { get; init; }
        public SemaphoreSlim InitLock { get; } = new(1, 1);
        public global::WTelegram.Client? Client { get; set; }
        public int RefCount { get; set; }
        public DateTimeOffset? IdleDeadlineUtc { get; set; }

        // 登录状态缓存（用于配置回调）
        public string? PendingPhone { get; set; }
        public string? PendingCode { get; set; }
        public string? PendingPassword { get; set; }
    }

    private readonly ConcurrentDictionary<long, ClientEntry> _entries = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WTelegramClientManager> _logger;
    private readonly WTelegramRuntimeOptions _options;

    public WTelegramClientManager(
        IServiceScopeFactory scopeFactory,
        IOptions<WTelegramRuntimeOptions> options,
        ILogger<WTelegramClientManager> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 获取指定账号的客户端租约
    /// </summary>
    public async Task<TdClientLease> AcquireAsync(long accountId, CancellationToken ct)
    {
        var entry = _entries.GetOrAdd(accountId, id => new ClientEntry { AccountId = id });

        await entry.InitLock.WaitAsync(ct);
        try
        {
            if (entry.Client is null)
                await CreateClientAsync(entry, ct);

            entry.RefCount++;
            entry.IdleDeadlineUtc = null;

            var adapter = new WTelegramClientAdapter(entry.Client!, accountId);
            return new TdClientLease(this, accountId, adapter);
        }
        finally
        {
            entry.InitLock.Release();
        }
    }

    /// <summary>
    /// 释放指定账号的客户端租约
    /// </summary>
    public async ValueTask ReleaseAsync(long accountId, CancellationToken ct)
    {
        if (!_entries.TryGetValue(accountId, out var entry))
            return;

        await entry.InitLock.WaitAsync(ct);
        try
        {
            if (entry.RefCount > 0)
                entry.RefCount--;

            if (entry.RefCount == 0)
                entry.IdleDeadlineUtc = DateTimeOffset.UtcNow.Add(_options.IdleTtl);
        }
        finally
        {
            entry.InitLock.Release();
        }
    }

    /// <summary>
    /// 清理过期的空闲客户端
    /// </summary>
    public async Task ReapExpiredAsync(CancellationToken ct)
    {
        foreach (var (accountId, entry) in _entries)
        {
            if (ct.IsCancellationRequested)
                return;

            await entry.InitLock.WaitAsync(ct);
            try
            {
                if (entry.Client is null || entry.RefCount != 0)
                    continue;

                if (entry.IdleDeadlineUtc is null || entry.IdleDeadlineUtc.Value > DateTimeOffset.UtcNow)
                    continue;

                await DisposeEntryAsync(accountId, entry);
            }
            finally
            {
                entry.InitLock.Release();
            }
        }
    }

    /// <summary>
    /// 设置待提交的手机号（用于登录流程）
    /// </summary>
    public void SetPendingPhone(long accountId, string phone)
    {
        var entry = _entries.GetOrAdd(accountId, id => new ClientEntry { AccountId = id });
        entry.PendingPhone = phone;
    }

    /// <summary>
    /// 设置待提交的验证码（用于登录流程）
    /// </summary>
    public void SetPendingCode(long accountId, string code)
    {
        var entry = _entries.GetOrAdd(accountId, id => new ClientEntry { AccountId = id });
        entry.PendingCode = code;
    }

    /// <summary>
    /// 设置待提交的 2FA 密码（用于登录流程）
    /// </summary>
    public void SetPendingPassword(long accountId, string password)
    {
        var entry = _entries.GetOrAdd(accountId, id => new ClientEntry { AccountId = id });
        entry.PendingPassword = password;
    }

    private async Task DisposeEntryAsync(long accountId, ClientEntry entry)
    {
        if (entry.Client is not null)
        {
            try
            {
                entry.Client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放 WTelegram 客户端 {AccountId} 时发生异常", accountId);
            }
            entry.Client = null;
        }

        entry.PendingPhone = null;
        entry.PendingCode = null;
        entry.PendingPassword = null;
        entry.IdleDeadlineUtc = null;
        _entries.TryRemove(accountId, out _);

        _logger.LogDebug("已释放空闲客户端: AccountId={AccountId}", accountId);

        await Task.CompletedTask;
    }

    private async Task CreateClientAsync(ClientEntry entry, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();
        var appConfigStore = scope.ServiceProvider.GetRequiredService<IAppConfigStore>();

        var account = await accountStore.GetAsync(entry.AccountId, ct);
        if (account is null)
            throw new NotFoundException($"账号不存在：{entry.AccountId}");

        var apiConfig = await appConfigStore.GetTelegramApiConfigAsync(ct);
        if (apiConfig is null || string.IsNullOrEmpty(apiConfig.ApiHash))
            throw new ValidationException("Telegram API 配置不完整，请先在设置中配置 API ID 和 API Hash");

        var apiId = account.ApiIdOverride ?? apiConfig.ApiId;
        var apiHash = apiConfig.ApiHash;

        Directory.CreateDirectory(account.DataDir);
        var sessionPath = Path.Combine(account.DataDir, "session.dat");

        // 创建 WTelegram 客户端
        var client = new global::WTelegram.Client(what => ConfigProvider(entry, what, apiId, apiHash, sessionPath));

        // 设置日志级别（减少控制台输出）
        global::WTelegram.Helpers.Log = (level, message) =>
        {
            if (level >= 2) // 仅记录警告及以上级别
            {
                _logger.LogDebug("[WTelegram] {Message}", message);
            }
        };

        entry.Client = client;

        _logger.LogInformation("已创建 WTelegram 客户端: AccountId={AccountId}, SessionPath={SessionPath}",
            entry.AccountId, sessionPath);

        // 尝试自动登录（如果已有会话）
        try
        {
            if (File.Exists(sessionPath))
            {
                var user = await client.LoginUserIfNeeded();
                _logger.LogInformation("账号 {AccountId} 自动登录成功: {Name}",
                    entry.AccountId, user.first_name);

                // 更新账号状态为 Ready（需要新的作用域因为前一个可能已释放）
                using var updateScope = _scopeFactory.CreateScope();
                var updateAccountStore = updateScope.ServiceProvider.GetRequiredService<IAccountStore>();
                await updateAccountStore.UpdateStatusAsync(entry.AccountId, AccountStatus.Ready, null, ct);
            }
        }
        catch (Exception ex)
        {
            // 首次登录或会话过期会失败，等待用户提交验证码
            _logger.LogDebug(ex, "账号 {AccountId} 自动登录失败，需要手动验证", entry.AccountId);
        }
    }

    private string? ConfigProvider(ClientEntry entry, string what, int apiId, string apiHash, string sessionPath)
    {
        return what switch
        {
            "api_id" => apiId.ToString(),
            "api_hash" => apiHash,
            "session_pathname" => sessionPath,
            "phone_number" => entry.PendingPhone,
            "verification_code" => entry.PendingCode,
            "password" => entry.PendingPassword,
            "device_model" => _options.DeviceModel,
            "system_version" => _options.SystemVersion,
            "app_version" => _options.ApplicationVersion,
            "system_lang_code" => _options.SystemLanguageCode,
            "lang_code" => _options.SystemLanguageCode,
            _ => null
        };
    }
}
