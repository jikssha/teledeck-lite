using Microsoft.Extensions.DependencyInjection;
using TgLitePanel.Core.Abstractions.Modules;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块系统服务注册扩展
/// </summary>
public static class ModuleServiceExtensions
{
    /// <summary>
    /// 添加模块系统服务
    /// </summary>
    public static IServiceCollection AddModuleSystem(
        this IServiceCollection services,
        Action<ModuleOptions>? configure = null)
    {
        services.Configure<ModuleOptions>(o =>
        {
            configure?.Invoke(o);
        });

        services.AddSingleton<ModuleValidator>();
        services.AddSingleton<ModuleManager>();
        services.AddSingleton<IModuleManager>(sp => sp.GetRequiredService<ModuleManager>());

        return services;
    }
}
