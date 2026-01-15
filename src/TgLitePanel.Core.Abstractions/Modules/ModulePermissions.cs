namespace TgLitePanel.Core.Abstractions.Modules;

/// <summary>
/// 模块权限常量定义
/// </summary>
public static class ModulePermissions
{
    // 账号相关
    public const string AccountsRead = "accounts.read";
    public const string AccountsWrite = "accounts.write";
    public const string AccountsDelete = "accounts.delete";

    // 消息相关
    public const string MessagesRead = "messages.read";
    public const string MessagesSend = "messages.send";
    public const string MessagesDelete = "messages.delete";

    // 存储相关
    public const string StorageLocal = "storage.local";
    public const string StorageDatabase = "storage.database";

    // 网络相关
    public const string NetworkHttp = "network.http";
    public const string NetworkTelegram = "network.telegram";

    // 系统相关
    public const string SystemSettings = "system.settings";
    public const string SystemAudit = "system.audit";

    /// <summary>
    /// 所有权限及其描述
    /// </summary>
    public static readonly IReadOnlyDictionary<string, PermissionInfo> All = new Dictionary<string, PermissionInfo>
    {
        [AccountsRead] = new("读取账号信息", PermissionLevel.Low),
        [AccountsWrite] = new("修改账号信息", PermissionLevel.Medium),
        [AccountsDelete] = new("删除账号", PermissionLevel.High),
        [MessagesRead] = new("读取消息", PermissionLevel.Medium),
        [MessagesSend] = new("发送消息", PermissionLevel.High),
        [MessagesDelete] = new("删除消息", PermissionLevel.High),
        [StorageLocal] = new("访问本地存储", PermissionLevel.Low),
        [StorageDatabase] = new("访问数据库", PermissionLevel.Medium),
        [NetworkHttp] = new("发起 HTTP 请求", PermissionLevel.Medium),
        [NetworkTelegram] = new("直接访问 Telegram API", PermissionLevel.High),
        [SystemSettings] = new("修改系统设置", PermissionLevel.High),
        [SystemAudit] = new("访问审计日志", PermissionLevel.Medium),
    };

    /// <summary>
    /// 获取权限描述
    /// </summary>
    public static string GetDescription(string permission)
    {
        return All.TryGetValue(permission, out var info)
            ? $"{permission} - {info.Description}"
            : permission;
    }
}

/// <summary>
/// 权限信息
/// </summary>
public sealed record PermissionInfo(string Description, PermissionLevel Level);

/// <summary>
/// 权限等级
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// 低风险 - 自动授予
    /// </summary>
    Low = 0,

    /// <summary>
    /// 中等风险 - 需用户确认
    /// </summary>
    Medium = 1,

    /// <summary>
    /// 高风险 - 需管理员审批
    /// </summary>
    High = 2
}
