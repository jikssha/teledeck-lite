using System.Reflection;
using System.Runtime.Loader;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块专用 AssemblyLoadContext，支持隔离加载和卸载
/// </summary>
public sealed class ModuleLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblies;
    private readonly string _moduleId;

    public ModuleLoadContext(
        string moduleId,
        string assemblyPath,
        IEnumerable<string> sharedAssemblies)
        : base(name: $"Module_{moduleId}", isCollectible: true)
    {
        _moduleId = moduleId;
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        _sharedAssemblies = new HashSet<string>(sharedAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 模块 ID
    /// </summary>
    public string ModuleId => _moduleId;

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 共享程序集由默认上下文提供
        if (_sharedAssemblies.Contains(assemblyName.Name ?? string.Empty))
        {
            return null; // 返回 null 让 runtime 从默认上下文加载
        }

        // 尝试从模块目录解析
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
