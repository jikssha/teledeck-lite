using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TgLitePanel.Core.Abstractions.Modules;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块运行时上下文实现
/// </summary>
internal sealed class ModuleContext : IModuleContext
{
    private readonly ModuleManifest _manifest;
    private readonly HashSet<string> _grantedPermissions;
    private readonly IServiceProvider _hostServices;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public ModuleContext(
        string installPath,
        ModuleManifest manifest,
        IEnumerable<string> grantedPermissions,
        IServiceProvider hostServices,
        ILogger logger,
        IConfiguration configuration)
    {
        InstallPath = installPath;
        _manifest = manifest;
        _grantedPermissions = new HashSet<string>(grantedPermissions, StringComparer.OrdinalIgnoreCase);
        _hostServices = hostServices;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string InstallPath { get; }

    /// <inheritdoc />
    public ModuleManifest Manifest => _manifest;

    /// <inheritdoc />
    public ILogger Logger => _logger;

    /// <inheritdoc />
    public IConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public bool HasPermission(string permission)
    {
        return _grantedPermissions.Contains(permission);
    }

    /// <inheritdoc />
    public T GetService<T>() where T : class
    {
        return _hostServices.GetRequiredService<T>();
    }

    /// <inheritdoc />
    public T? GetServiceOrDefault<T>() where T : class
    {
        return _hostServices.GetService<T>();
    }
}
