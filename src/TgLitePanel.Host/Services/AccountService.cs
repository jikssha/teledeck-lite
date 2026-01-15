using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using TgLitePanel.Core.Abstractions.Exceptions;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Core.Abstractions.TdLib;
using TgLitePanel.Core.Abstractions.Utils;

namespace TgLitePanel.Host.Services;

public sealed class AccountService : IAccountService
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+][0-9]{6,20}$", RegexOptions.Compiled);
    private static readonly Regex CodeRegex = new(@"^[0-9]{2,10}$", RegexOptions.Compiled);

    private readonly IAccountStore _accountStore;
    private readonly ITdClientManager _tdClientManager;
    private readonly AppRuntimeOptions _runtime;

    public AccountService(IAccountStore accountStore, ITdClientManager tdClientManager, AppRuntimeOptions runtime)
    {
        _accountStore = accountStore;
        _tdClientManager = tdClientManager;
        _runtime = runtime;
    }

    public async Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await _accountStore.ListAsync(cancellationToken);
        return list.Select(x => new AccountDto(x.AccountId, x.Phone, x.Status, x.DataDir, x.ApiIdOverride, x.SystemChatId)).ToList();
    }

    public async Task<AccountDto> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var account = await _accountStore.GetAsync(accountId, cancellationToken);
        if (account is null)
            throw new NotFoundException($"账号不存在：{accountId}");

        return new AccountDto(account.AccountId, account.Phone, account.Status, account.DataDir, account.ApiIdOverride, account.SystemChatId);
    }

    public async Task<long> StartLoginAsync(string phone, CancellationToken cancellationToken)
    {
        phone = phone.Trim();
        if (!PhoneRegex.IsMatch(phone))
            throw new ValidationException("手机号格式不正确。");

        var placeholderDir = Path.Combine(_runtime.DataDir, "accounts", "pending");
        var accountId = await _accountStore.CreateAsync(phone, placeholderDir, AccountStatus.Authorizing, cancellationToken);

        var finalDir = Path.Combine(_runtime.DataDir, "accounts", accountId.ToString());
        await _accountStore.UpdateDataDirAsync(accountId, finalDir, cancellationToken);

        Directory.CreateDirectory(finalDir);

        _tdClientManager.SetPendingPhone(accountId, phone);
        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var request = new JsonObject
        {
            ["@type"] = "setAuthenticationPhoneNumber",
            ["phone_number"] = phone,
            ["settings"] = new JsonObject
            {
                ["@type"] = "phoneNumberAuthenticationSettings",
                ["allow_flash_call"] = false,
                ["is_current_phone_number"] = false,
                ["allow_sms_retriever_api"] = false
            }
        }.ToJsonString();

        _ = await lease.Client.ExecuteAsync(request, TimeSpan.FromSeconds(15), cancellationToken);
        return accountId;
    }

    public async Task ResendCodeAsync(long accountId, CancellationToken cancellationToken)
    {
        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var request = new JsonObject { ["@type"] = "resendAuthenticationCode" }.ToJsonString();
        _ = await lease.Client.ExecuteAsync(request, TimeSpan.FromSeconds(15), cancellationToken);
    }

    public async Task SubmitLoginCodeAsync(long accountId, string code, CancellationToken cancellationToken)
    {
        code = code.Trim();
        if (!CodeRegex.IsMatch(code))
            throw new ValidationException("验证码格式不正确。");

        _tdClientManager.SetPendingCode(accountId, code);
        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var request = new JsonObject { ["@type"] = "checkAuthenticationCode", ["code"] = code }.ToJsonString();
        _ = await lease.Client.ExecuteAsync(request, TimeSpan.FromSeconds(15), cancellationToken);

        if (await TryMarkReadyAsync(accountId, lease.Client, cancellationToken))
            await _accountStore.UpdateStatusAsync(accountId, AccountStatus.Ready, null, cancellationToken);
    }

    public async Task SubmitLoginPasswordAsync(long accountId, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
            throw new ValidationException("2FA 密码不能为空。");

        _tdClientManager.SetPendingPassword(accountId, password);
        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var request = new JsonObject { ["@type"] = "checkAuthenticationPassword", ["password"] = password }.ToJsonString();
        _ = await lease.Client.ExecuteAsync(request, TimeSpan.FromSeconds(15), cancellationToken);

        if (await TryMarkReadyAsync(accountId, lease.Client, cancellationToken))
            await _accountStore.UpdateStatusAsync(accountId, AccountStatus.Ready, null, cancellationToken);
    }

    public async Task<Stream> ExportZipAsync(long accountId, CancellationToken cancellationToken)
    {
        var account = await _accountStore.GetAsync(accountId, cancellationToken);
        if (account is null)
            throw new NotFoundException($"账号不存在：{accountId}");

        var tempPath = Path.Combine(Path.GetTempPath(), $"tglitepanel-export-{accountId}-{Guid.NewGuid():N}.zip");
        await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
        {
            await AddSessionFilesToZipAsync(zip, account.Phone, account.DataDir, cancellationToken);
        }

        return new TempFileStream(tempPath);
    }

    public async Task<Stream> ExportAllZipAsync(CancellationToken cancellationToken)
    {
        var accounts = await _accountStore.ListAsync(cancellationToken);
        var tempPath = Path.Combine(Path.GetTempPath(), $"tglitepanel-export-all-{Guid.NewGuid():N}.zip");

        await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var account in accounts)
            {
                await AddSessionFilesToZipAsync(zip, account.Phone, account.DataDir, cancellationToken);
            }
        }

        return new TempFileStream(tempPath);
    }

    /// <summary>
    /// 将账号的会话文件添加到 ZIP 归档
    /// 格式：{phone}/{phone}.session + {phone}/{phone}.json
    /// </summary>
    private static async Task AddSessionFilesToZipAsync(
        ZipArchive zip,
        string phone,
        string dataDir,
        CancellationToken cancellationToken)
    {
        var sessionPath = Path.Combine(dataDir, "session.dat");
        if (!File.Exists(sessionPath))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        // 添加 .session 文件
        var sessionZipPath = $"{phone}/{phone}.session";
        var sessionEntry = zip.CreateEntry(sessionZipPath, CompressionLevel.Fastest);
        await using (var entryStream = sessionEntry.Open())
        await using (var fileStream = new FileStream(sessionPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }

        // 添加元数据 .json 文件
        var metadata = new
        {
            phone,
            exportedAtUtc = DateTime.UtcNow
        };

        var jsonZipPath = $"{phone}/{phone}.json";
        var jsonEntry = zip.CreateEntry(jsonZipPath, CompressionLevel.Fastest);
        await using var jsonStream = jsonEntry.Open();
        await JsonSerializer.SerializeAsync(jsonStream, metadata, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<long>> ImportZipAsync(Stream zipStream, long zipLength, CancellationToken cancellationToken)
    {
        if (zipLength <= 0 || zipLength > SessionZipImport.MaxZipBytes)
            throw new ValidationException("导入文件大小超出限制。");

        var tempDir = Path.Combine(_runtime.DataDir, "tmp");
        Directory.CreateDirectory(tempDir);
        var tempZip = Path.Combine(tempDir, $"tglitepanel-import-{Guid.NewGuid():N}.zip");
        await using (var fs = new FileStream(tempZip, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            await zipStream.CopyToAsync(fs, cancellationToken);
        }

        var extractDir = Path.Combine(tempDir, $"extract-{Guid.NewGuid():N}");

        try
        {
            await using var read = new FileStream(tempZip, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var zip = new ZipArchive(read, ZipArchiveMode.Read, leaveOpen: false);

            // ZIP 炸弹安全验证
            SessionZipImport.ValidateZipSecurity(zip, zipLength);

            // 解析账号信息
            var parsedAccounts = await SessionZipImport.ParseAccountsFromZipAsync(zip, extractDir, cancellationToken);
            if (parsedAccounts.Count == 0)
                throw new ValidationException("未找到可导入的账号（需要 .session 文件）。");

            var imported = new List<long>();

            foreach (var accountInfo in parsedAccounts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 创建账号记录（带二级密码）
                var placeholderDir = Path.Combine(_runtime.DataDir, "accounts", "pending");
                var accountId = await _accountStore.CreateAsync(
                    accountInfo.Phone,
                    placeholderDir,
                    AccountStatus.Ready,
                    cancellationToken);

                // 设置最终目录
                var finalDir = Path.Combine(_runtime.DataDir, "accounts", accountId.ToString());
                await _accountStore.UpdateDataDirAsync(accountId, finalDir, cancellationToken);
                Directory.CreateDirectory(finalDir);

                // 复制 session 文件到最终目录
                var destSessionPath = Path.Combine(finalDir, "session.dat");
                File.Copy(accountInfo.SessionFilePath, destSessionPath, overwrite: true);

                imported.Add(accountId);
            }

            return imported;
        }
        finally
        {
            try { File.Delete(tempZip); } catch { }
            try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, recursive: true); } catch { }
        }
    }

    private static async Task<bool> TryMarkReadyAsync(long accountId, ITdClient client, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.ExecuteAsync("{\"@type\":\"getMe\"}", TimeSpan.FromSeconds(10), cancellationToken);
            using var doc = TdJsonHelpers.Parse(response);
            TdJsonHelpers.ThrowIfError(doc.RootElement);
            return doc.RootElement.TryGetProperty("@type", out var t) && t.GetString() == "user";
        }
        catch
        {
            return false;
        }
    }
}
