namespace TgLitePanel.Infrastructure.WTelegram;

/// <summary>
/// WTelegram 客户端运行时配置选项
/// </summary>
public sealed class WTelegramRuntimeOptions
{
    /// <summary>
    /// 客户端空闲后的存活时间（默认 5 分钟）
    /// </summary>
    public TimeSpan IdleTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 清理服务的运行间隔（默认 10 秒）
    /// </summary>
    public TimeSpan ReapInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 设备型号（默认 "TgLitePanel"）
    /// </summary>
    public string DeviceModel { get; set; } = "TgLitePanel";

    /// <summary>
    /// 系统版本（默认 "Linux"）
    /// </summary>
    public string SystemVersion { get; set; } = "Linux";

    /// <summary>
    /// 应用版本（默认 "1.0"）
    /// </summary>
    public string ApplicationVersion { get; set; } = "1.0";

    /// <summary>
    /// 系统语言代码（默认 "zh-CN"）
    /// </summary>
    public string SystemLanguageCode { get; set; } = "zh-CN";
}
