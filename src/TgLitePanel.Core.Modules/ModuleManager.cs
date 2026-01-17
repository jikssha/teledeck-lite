using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgLitePanel.Core.Abstractions.Modules;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块管理器实现
/// </summary>
public sealed class ModuleManager : IModuleManager, IAsyncDisposable
{
    private readonly ModuleValidator _validator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _hostServices;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModuleManager> _logger;
    private readonly ModuleOptions _options;
    private readonly string _modulesPath;

    private readonly ConcurrentDictionary<string, LoadedModuleInstance> _loadedModules = new();
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public event EventHandler<ModuleStatusChangedEventArgs>? ModuleStatusChanged;

    public ModuleManager(
        ModuleValidator validator,
        IServiceScopeFactory scopeFactory,
        IServiceProvider hostServices,
        IConfiguration configuration,
        IOptions<ModuleOptions> options,
        ILogger<ModuleManager> logger)
    {
        _validator = validator;
        _scopeFactory = scopeFactory;
        _hostServices = hostServices;
        _configuration = configuration;
        _logger = logger;
        _options = options.Value;

        var dataDir = configuration["DATA_DIR"] ?? "data";
        _modulesPath = Path.GetFullPath(Path.Combine(dataDir, _options.ModulesDirectory));
        Directory.CreateDirectory(_modulesPath);
    }

    /// <summary>
    /// 获取模块存储（在作用域内使用）
    /// </summary>
    private IModuleStore GetModuleStore(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<IModuleStore>();

    private static string NormalizeDirectoryPath(string path)
    {
        var full = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(full);
    }

    private string GetExpectedInstallPath(string moduleId)
        => NormalizeDirectoryPath(Path.Combine(_modulesPath, moduleId));

    private bool IsValidInstallPath(ModuleInfo module, out string expectedInstallPath, out string actualInstallPath)
    {
        expectedInstallPath = GetExpectedInstallPath(module.Id);
        actualInstallPath = NormalizeDirectoryPath(module.InstallPath);
        return string.Equals(actualInstallPath, expectedInstallPath, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<ModuleValidationResult> ValidateModuleAsync(
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        return await _validator.ValidateAsync(zipStream, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ModuleInstallResult> InstallModuleAsync(
        Stream zipStream,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // 1. 验证模块
            var validation = await _validator.ValidateAsync(zipStream, cancellationToken);

            if (!validation.IsValid || validation.Manifest == null)
            {
                return new ModuleInstallResult(
                    false,
                    null,
                    validation.Errors ?? new List<string> { "验证失败" },
                    validation.Warnings);
            }

            var manifest = validation.Manifest;
            warnings.AddRange(validation.Warnings ?? Enumerable.Empty<string>());

            // 2. 检查是否已安装
            using var scope = _scopeFactory.CreateScope();
            var store = GetModuleStore(scope);
            var existing = await store.GetModuleAsync(manifest.Id, cancellationToken);
            if (existing != null)
            {
                // 如果模块正在运行，先卸载
                if (_loadedModules.TryGetValue(manifest.Id, out var loaded))
                {
                    await loaded.DisposeAsync();
                    _loadedModules.TryRemove(manifest.Id, out _);
                }

                // 删除旧目录
                var oldPath = Path.Combine(_modulesPath, manifest.Id);
                if (Directory.Exists(oldPath))
                {
                    Directory.Delete(oldPath, true);
                }

                warnings.Add($"已替换旧版本 {existing.Version}");
            }

            // 3. 解压到目标目录
            var installPath = Path.Combine(_modulesPath, manifest.Id);
            Directory.CreateDirectory(installPath);
            var fullInstallPath = NormalizeDirectoryPath(installPath);
            var fullInstallPathPrefix = fullInstallPath + Path.DirectorySeparatorChar;

            if (zipStream.CanSeek)
            {
                zipStream.Position = 0;
            }

            var checksum = ModuleValidator.ComputeChecksum(zipStream);

            if (zipStream.CanSeek)
            {
                zipStream.Position = 0;
            }

            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destPath = Path.Combine(installPath, entry.FullName);
                    var fullDestPath = Path.GetFullPath(destPath);
                    if (!fullDestPath.StartsWith(fullInstallPathPrefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"检测到非法模块路径（Zip-Slip）：{entry.FullName}");

                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }

            // 4. 保存模块信息到数据库
            var moduleInfo = new ModuleInfo
            {
                Id = manifest.Id,
                Name = manifest.Name,
                Version = manifest.Version,
                Description = manifest.Description,
                AuthorName = manifest.Author?.Name,
                AuthorEmail = manifest.Author?.Email,
                Status = ModuleStatus.Disabled,
                InstallPath = installPath,
                EntryAssembly = manifest.EntryPoint.Assembly,
                EntryType = manifest.EntryPoint.Type,
                InstalledAt = DateTime.UtcNow,
                InstalledBy = userId,
                Checksum = checksum,
                Permissions = manifest.Permissions ?? new List<string>(),
                Routes = manifest.Routes ?? new List<ModuleRoute>()
            };

            await store.SaveModuleAsync(moduleInfo, cancellationToken);

            // 5. 记录审计日志
            await store.AddAuditLogAsync(
                manifest.Id,
                "installed",
                new { manifest.Version, manifest.Permissions },
                userId,
                cancellationToken);

            _logger.LogInformation("模块已安装：{ModuleId} v{Version}", manifest.Id, manifest.Version);

            return new ModuleInstallResult(
                true,
                moduleInfo,
                null,
                warnings.Count > 0 ? warnings : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装模块时发生错误");
            errors.Add($"安装失败：{ex.Message}");
            return new ModuleInstallResult(false, null, errors, warnings);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UninstallModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 停止并卸载模块
            if (_loadedModules.TryRemove(moduleId, out var loaded))
            {
                await loaded.DisposeAsync();
            }

            // 2. 获取模块信息
            using var scope = _scopeFactory.CreateScope();
            var store = GetModuleStore(scope);
            var module = await store.GetModuleAsync(moduleId, cancellationToken);
            if (module == null)
            {
                return false;
            }

            // 3. 删除文件
            if (!IsValidInstallPath(module, out var expected, out var actual))
            {
                _logger.LogError(
                    "拒绝卸载模块：InstallPath 非法或已被篡改。ModuleId={ModuleId}, Expected={Expected}, Actual={Actual}",
                    moduleId, expected, actual);
                return false;
            }

            if (Directory.Exists(expected))
            {
                Directory.Delete(expected, true);
            }

            // 4. 从数据库删除
            await store.DeleteModuleAsync(moduleId, cancellationToken);

            // 5. 记录审计日志
            await store.AddAuditLogAsync(
                moduleId,
                "uninstalled",
                new { module.Version },
                null,
                cancellationToken);

            _logger.LogInformation("模块已卸载：{ModuleId}", moduleId);

            ModuleStatusChanged?.Invoke(this, new ModuleStatusChangedEventArgs(
                moduleId, ModuleStatus.Disabled, module.Status));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卸载模块 {ModuleId} 时发生错误", moduleId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> EnableModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var store = GetModuleStore(scope);
            var module = await store.GetModuleAsync(moduleId, cancellationToken);
            if (module == null)
            {
                _logger.LogWarning("尝试启用不存在的模块：{ModuleId}", moduleId);
                return false;
            }

            if (_loadedModules.ContainsKey(moduleId))
            {
                _logger.LogInformation("模块已启用：{ModuleId}", moduleId);
                return true;
            }

            try
            {
                var instance = await LoadModuleAsync(module, cancellationToken);
                _loadedModules[moduleId] = instance;

                await store.UpdateModuleStatusAsync(moduleId, ModuleStatus.Enabled, null, cancellationToken);
                await store.UpdateLastLoadedTimeAsync(moduleId, DateTime.UtcNow, cancellationToken);

                await store.AddAuditLogAsync(
                    moduleId,
                    "enabled",
                    null,
                    null,
                    cancellationToken);

                _logger.LogInformation("模块已启用：{ModuleId}", moduleId);

                ModuleStatusChanged?.Invoke(this, new ModuleStatusChangedEventArgs(
                    moduleId, ModuleStatus.Enabled, module.Status));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载模块 {ModuleId} 失败", moduleId);

                await store.UpdateModuleStatusAsync(
                    moduleId,
                    ModuleStatus.Error,
                    ex.Message,
                    cancellationToken);

                ModuleStatusChanged?.Invoke(this, new ModuleStatusChangedEventArgs(
                    moduleId, ModuleStatus.Error, module.Status));

                return false;
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisableModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var store = GetModuleStore(scope);
            var module = await store.GetModuleAsync(moduleId, cancellationToken);
            if (module == null)
            {
                return false;
            }

            if (_loadedModules.TryRemove(moduleId, out var instance))
            {
                await instance.DisposeAsync();
            }

            await store.UpdateModuleStatusAsync(moduleId, ModuleStatus.Disabled, null, cancellationToken);

            await store.AddAuditLogAsync(
                moduleId,
                "disabled",
                null,
                null,
                cancellationToken);

            _logger.LogInformation("模块已禁用：{ModuleId}", moduleId);

            ModuleStatusChanged?.Invoke(this, new ModuleStatusChangedEventArgs(
                moduleId, ModuleStatus.Disabled, module.Status));

            return true;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModuleInfo>> GetAllModulesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = GetModuleStore(scope);
        return await store.GetAllModulesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ModuleInfo?> GetModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = GetModuleStore(scope);
        return await store.GetModuleAsync(moduleId, cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<Assembly> GetLoadedModuleAssemblies()
    {
        return _loadedModules.Values
            .Where(m => m.IsActivated)
            .Select(m => m.EntryAssembly);
    }

    /// <inheritdoc />
    public IEnumerable<ModuleMenuItem> GetModuleMenuItems()
    {
        return _loadedModules.Values
            .Where(m => m.IsActivated)
            .SelectMany(m => m.EntryPoint.GetMenuItems());
    }

    /// <inheritdoc />
    public async Task LoadEnabledModulesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var store = GetModuleStore(scope);
        var enabledModules = await store.GetEnabledModulesAsync(cancellationToken);

        foreach (var module in enabledModules)
        {
            try
            {
                await EnableModuleAsync(module.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动时加载模块 {ModuleId} 失败", module.Id);
            }
        }
    }

    private async Task<LoadedModuleInstance> LoadModuleAsync(
        ModuleInfo module,
        CancellationToken cancellationToken)
    {
        if (!IsValidInstallPath(module, out var expected, out var actual))
        {
            throw new InvalidOperationException(
                $"模块 InstallPath 非法或已被篡改：ModuleId={module.Id}, Expected={expected}, Actual={actual}");
        }

        var assemblyPath = Path.Combine(module.InstallPath, module.EntryAssembly);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"入口程序集不存在：{assemblyPath}");
        }

        // 读取清单
        var manifestPath = Path.Combine(module.InstallPath, "manifest.json");
        ModuleManifest manifest;

        using (var fs = File.OpenRead(manifestPath))
        {
            manifest = await JsonSerializer.DeserializeAsync<ModuleManifest>(fs, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("无法解析清单文件");
        }

        // 创建隔离的加载上下文
        var loadContext = new ModuleLoadContext(
            module.Id,
            assemblyPath,
            _options.SharedAssemblies);

        // 加载入口程序集
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

        // 查找入口类型
        var entryType = assembly.GetType(module.EntryType);
        if (entryType == null)
        {
            loadContext.Unload();
            throw new TypeLoadException($"找不到入口类型：{module.EntryType}");
        }

        // 创建入口点实例
        var entryPoint = Activator.CreateInstance(entryType) as IModuleEntryPoint;
        if (entryPoint == null)
        {
            loadContext.Unload();
            throw new InvalidOperationException($"入口类型未实现 IModuleEntryPoint：{module.EntryType}");
        }

        // 创建模块上下文
        var moduleLogger = _hostServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger($"Module.{module.Id}");

        var moduleConfig = _configuration.GetSection($"Modules:{module.Id}");

        var context = new ModuleContext(
            module.InstallPath,
            manifest,
            module.Permissions,
            _hostServices,
            moduleLogger,
            moduleConfig);

        // 初始化模块
        await entryPoint.InitializeAsync(context, cancellationToken);

        // 激活模块
        await entryPoint.ActivateAsync(cancellationToken);

        return new LoadedModuleInstance
        {
            ModuleId = module.Id,
            LoadContext = loadContext,
            EntryAssembly = assembly,
            EntryPoint = entryPoint,
            Manifest = manifest,
            IsActivated = true
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (moduleId, instance) in _loadedModules)
        {
            try
            {
                await instance.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "卸载模块 {ModuleId} 时发生错误", moduleId);
            }
        }

        _loadedModules.Clear();
        _loadLock.Dispose();
    }
}
