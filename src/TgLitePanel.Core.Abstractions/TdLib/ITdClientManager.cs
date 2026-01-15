namespace TgLitePanel.Core.Abstractions.TdLib;

public interface ITdClientManager
{
    Task<TdClientLease> AcquireAsync(long accountId, CancellationToken cancellationToken);
    ValueTask ReleaseAsync(long accountId, CancellationToken cancellationToken);

    /// <summary>
    /// 设置待提交的手机号（用于登录流程）
    /// </summary>
    void SetPendingPhone(long accountId, string phone);

    /// <summary>
    /// 设置待提交的验证码（用于登录流程）
    /// </summary>
    void SetPendingCode(long accountId, string code);

    /// <summary>
    /// 设置待提交的 2FA 密码（用于登录流程）
    /// </summary>
    void SetPendingPassword(long accountId, string password);
}
