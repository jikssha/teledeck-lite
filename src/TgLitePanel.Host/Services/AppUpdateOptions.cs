namespace TgLitePanel.Host.Services;

/// <summary>
/// 应用更新检查配置
/// </summary>
public sealed class AppUpdateOptions
{
    /// <summary>
    /// 仓库地址（例如：https://github.com/owner/repo）
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// 检查间隔（分钟）
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 60;
}

