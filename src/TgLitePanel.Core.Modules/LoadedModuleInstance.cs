using System.Reflection;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 已加载的模块实例
/// </summary>
internal sealed class LoadedModuleInstance : IAsyncDisposable
{
    public required string ModuleId { get; init; }
    public required ModuleLoadContext LoadContext { get; init; }
    public required Assembly EntryAssembly { get; init; }
    public required TgLitePanel.Core.Abstractions.Modules.IModuleEntryPoint EntryPoint { get; init; }
    public required TgLitePanel.Core.Abstractions.Modules.ModuleManifest Manifest { get; init; }
    public bool IsActivated { get; set; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (IsActivated)
            {
                await EntryPoint.DeactivateAsync();
                IsActivated = false;
            }
        }
        catch
        {
            // 忽略停用时的错误
        }

        // 卸载 AssemblyLoadContext
        LoadContext.Unload();

        // 触发 GC 以确保程序集被卸载
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
