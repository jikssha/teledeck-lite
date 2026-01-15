namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// Webhook 配置
/// </summary>
public sealed class WebhookConfig
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? Secret { get; set; }
    public bool IsEnabled { get; set; }
    public List<string> Events { get; set; } = new();
    public List<long> AccountIds { get; set; } = new();
    public int RetryCount { get; set; }
    public int TimeoutSeconds { get; set; }
    public DateTime? LastTriggeredAtUtc { get; set; }
    public string? LastError { get; set; }
}
