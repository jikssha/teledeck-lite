namespace TgLitePanel.Core.Abstractions;

public static class AppConfigKeys
{
    public const string TgApiId = "TgApiId";
    public const string TgApiHash = "TgApiHash";

    // 安全策略：是否要求管理员在 Web 端修改默认账号/密码
    public const string SecurityMustChangeAdminCredentials = "SecurityMustChangeAdminCredentials";
}
