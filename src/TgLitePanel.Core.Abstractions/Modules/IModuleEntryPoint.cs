using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TgLitePanel.Core.Abstractions.Modules;

/// <summary>
/// 模块入口点接口，所有动态模块必须实现此接口
/// </summary>
public interface IModuleEntryPoint
{
    /// <summary>
    /// 模块唯一标识符
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// 模块初始化，在加载后立即调用
    /// </summary>
    Task InitializeAsync(IModuleContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 模块激活，在启用时调用
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 模块停用，在禁用或卸载前调用
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 注册模块服务到 DI 容器
    /// </summary>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// 获取模块提供的路由组件
    /// </summary>
    IEnumerable<Type> GetRoutableComponents();

    /// <summary>
    /// 获取导航菜单项
    /// </summary>
    IEnumerable<ModuleMenuItem> GetMenuItems();
}

/// <summary>
/// 模块上下文，提供模块运行时所需的服务和配置
/// </summary>
public interface IModuleContext
{
    /// <summary>
    /// 模块安装路径
    /// </summary>
    string InstallPath { get; }

    /// <summary>
    /// 模块清单
    /// </summary>
    ModuleManifest Manifest { get; }

    /// <summary>
    /// 日志记录器
    /// </summary>
    Microsoft.Extensions.Logging.ILogger Logger { get; }

    /// <summary>
    /// 模块配置
    /// </summary>
    Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }

    /// <summary>
    /// 检查模块是否有指定权限
    /// </summary>
    bool HasPermission(string permission);

    /// <summary>
    /// 获取宿主服务
    /// </summary>
    T GetService<T>() where T : class;

    /// <summary>
    /// 获取宿主服务（可空）
    /// </summary>
    T? GetServiceOrDefault<T>() where T : class;
}

/// <summary>
/// 模块导航菜单项
/// </summary>
public sealed record ModuleMenuItem(
    string Title,
    string Href,
    string Icon,
    int Order = 0,
    string? ParentId = null,
    bool ShowInNav = true
);
