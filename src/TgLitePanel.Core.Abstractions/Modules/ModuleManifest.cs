using System.Text.Json.Serialization;

namespace TgLitePanel.Core.Abstractions.Modules;

/// <summary>
/// 模块清单定义
/// </summary>
public sealed class ModuleManifest
{
    /// <summary>
    /// 模块唯一标识符 (例如: com.example.mymodule)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 模块显示名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模块版本 (SemVer)
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 模块描述
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 作者信息
    /// </summary>
    [JsonPropertyName("author")]
    public ModuleAuthor? Author { get; set; }

    /// <summary>
    /// 许可证类型
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }

    /// <summary>
    /// 入口点配置
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public ModuleEntryPointConfig EntryPoint { get; set; } = new();

    /// <summary>
    /// 兼容性要求
    /// </summary>
    [JsonPropertyName("compatibility")]
    public ModuleCompatibility? Compatibility { get; set; }

    /// <summary>
    /// 依赖的其他模块
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<ModuleDependency>? Dependencies { get; set; }

    /// <summary>
    /// 请求的权限列表
    /// </summary>
    [JsonPropertyName("permissions")]
    public List<string>? Permissions { get; set; }

    /// <summary>
    /// 模块提供的路由
    /// </summary>
    [JsonPropertyName("routes")]
    public List<ModuleRoute>? Routes { get; set; }

    /// <summary>
    /// 模块设置配置
    /// </summary>
    [JsonPropertyName("settings")]
    public ModuleSettings? Settings { get; set; }
}

/// <summary>
/// 模块作者信息
/// </summary>
public sealed class ModuleAuthor
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// 模块入口点配置
/// </summary>
public sealed class ModuleEntryPointConfig
{
    /// <summary>
    /// 主程序集文件名 (例如: MyModule.dll)
    /// </summary>
    [JsonPropertyName("assembly")]
    public string Assembly { get; set; } = string.Empty;

    /// <summary>
    /// 入口类完整类型名 (例如: MyModule.ModuleEntry)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 模块兼容性要求
/// </summary>
public sealed class ModuleCompatibility
{
    /// <summary>
    /// 最低宿主版本
    /// </summary>
    [JsonPropertyName("minHostVersion")]
    public string? MinHostVersion { get; set; }

    /// <summary>
    /// 最高宿主版本
    /// </summary>
    [JsonPropertyName("maxHostVersion")]
    public string? MaxHostVersion { get; set; }

    /// <summary>
    /// 目标框架 (例如: net8.0)
    /// </summary>
    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }
}

/// <summary>
/// 模块依赖
/// </summary>
public sealed class ModuleDependency
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// 模块路由定义
/// </summary>
public sealed class ModuleRoute
{
    /// <summary>
    /// 路由路径 (例如: /mymodule)
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 显示标题
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Material 图标名称
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否在导航菜单中显示
    /// </summary>
    [JsonPropertyName("showInNav")]
    public bool ShowInNav { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    [JsonPropertyName("order")]
    public int Order { get; set; } = 100;
}

/// <summary>
/// 模块设置配置
/// </summary>
public sealed class ModuleSettings
{
    /// <summary>
    /// 是否可配置
    /// </summary>
    [JsonPropertyName("configurable")]
    public bool Configurable { get; set; }

    /// <summary>
    /// 设置组件类型名
    /// </summary>
    [JsonPropertyName("settingsComponent")]
    public string? SettingsComponent { get; set; }
}
