namespace TgLitePanel.Core.Abstractions.Services;

/// <summary>
/// 账号健康检查进度事件
/// </summary>
public sealed record HealthCheckProgressEvent
{
    /// <summary>
    /// 批次 ID
    /// </summary>
    public required string BatchId { get; init; }

    /// <summary>
    /// 当前检查的账号 ID
    /// </summary>
    public long CurrentAccountId { get; init; }

    /// <summary>
    /// 已完成数量
    /// </summary>
    public int Completed { get; init; }

    /// <summary>
    /// 总数量
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsFinished { get; init; }

    /// <summary>
    /// 当前账号的检查结果
    /// </summary>
    public bool? IsOnline { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// 健康检查请求
/// </summary>
public sealed record HealthCheckRequest
{
    /// <summary>
    /// 批次 ID（可选；若未指定，服务端会自动生成）
    /// </summary>
    public string? BatchId { get; init; }

    /// <summary>
    /// 要检查的账号 ID 列表（为空则检查全部）
    /// </summary>
    public long[]? AccountIds { get; init; }

    /// <summary>
    /// 要检查的分组 ID 列表（为空则不按分组过滤）
    /// </summary>
    public long[]? GroupIds { get; init; }

    /// <summary>
    /// 检查来源
    /// </summary>
    public string Source { get; init; } = "manual";
}

/// <summary>
/// 健康检查结果
/// </summary>
public sealed record HealthCheckResult
{
    public required string BatchId { get; init; }
    public int TotalChecked { get; init; }
    public int OnlineCount { get; init; }
    public int OfflineCount { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime FinishedAtUtc { get; init; }
}

/// <summary>
/// 账号健康检查服务接口
/// </summary>
public interface IAccountHealthCheckService
{
    /// <summary>
    /// 触发手动健康检查
    /// </summary>
    Task<string> TriggerCheckAsync(HealthCheckRequest request, CancellationToken ct);

    /// <summary>
    /// 获取当前检查进度
    /// </summary>
    HealthCheckProgressEvent? GetCurrentProgress();

    /// <summary>
    /// 检查是否有正在运行的检查任务
    /// </summary>
    bool IsRunning { get; }
}
