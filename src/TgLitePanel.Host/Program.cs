using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TgLitePanel.Core.Abstractions;
using TgLitePanel.Core.Abstractions.Modules;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Core.Abstractions.TdLib;
using TgLitePanel.Core.Modules;
using TgLitePanel.Host.Components;
using TgLitePanel.Host.Hubs;
using TgLitePanel.Host.Services;
using TgLitePanel.Infrastructure.Persistence;
using TgLitePanel.Infrastructure.Persistence.Stores;
using TgLitePanel.Infrastructure.WTelegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// 反向代理支持：用于 Caddy/Nginx 等终止 HTTPS 的场景
// - 仅信任来自私网/本机的转发头，避免公网直接伪造 X-Forwarded-* 影响 Scheme/Host 判定
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    o.ForwardLimit = 2;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();

    o.KnownProxies.Add(System.Net.IPAddress.Loopback);
    o.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
    o.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
    o.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
    o.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16));
});

builder.Services.Configure<AppUpdateOptions>(builder.Configuration.GetSection("AppUpdate"));
builder.Services.AddSingleton<AppUpdateService>();
builder.Services.AddHostedService<AppUpdateCheckerHostedService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.Cookie.Name = "tglitepanel";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        // 允许 README 的 http://IP:7070 直连场景写入 Cookie；反代 HTTPS 时会自动变为 Secure Cookie
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = TimeSpan.FromDays(30);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var dataDir = Environment.GetEnvironmentVariable("DATA_DIR");
if (string.IsNullOrWhiteSpace(dataDir))
    dataDir = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "data");

dataDir = Path.GetFullPath(dataDir);
Directory.CreateDirectory(dataDir);

var dbPath = Environment.GetEnvironmentVariable("DB_PATH");
if (string.IsNullOrWhiteSpace(dbPath))
    dbPath = Path.Combine(dataDir, "app.db");

dbPath = Path.GetFullPath(dbPath);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

// 统一数据目录配置：确保模块系统等使用与宿主一致的 DATA_DIR/DB_PATH
builder.Configuration["DATA_DIR"] = dataDir;
builder.Configuration["DB_PATH"] = dbPath;

builder.Services.AddSingleton(new AppRuntimeOptions
{
    DataDir = dataDir,
    DbPath = dbPath
});

var sqliteConnectionString = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate;";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(sqliteConnectionString));

builder.Services.AddScoped<IAppConfigStore, EfAppConfigStore>();
builder.Services.AddScoped<IAccountStore, EfAccountStore>();
builder.Services.AddScoped<IUserStore, EfUserStore>();
builder.Services.AddScoped<IAuditLogStore, EfAuditLogStore>();
builder.Services.AddScoped<ISharedCodeStore, EfSharedCodeStore>();
builder.Services.AddScoped<IAccountGroupStore, EfAccountGroupStore>();
builder.Services.AddScoped<IMessageCacheStore, EfMessageCacheStore>();
builder.Services.AddScoped<IWebhookConfigStore, EfWebhookConfigStore>();
builder.Services.AddScoped<IModuleStore, ModuleStore>();
builder.Services.AddScoped<IAccountStatusLogStore, EfAccountStatusLogStore>();
builder.Services.AddScoped<IAlertConfigStore, EfAlertConfigStore>();
builder.Services.AddScoped<IAlertHistoryStore, EfAlertHistoryStore>();

builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IAppConfigService, AppConfigService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ISystemNotificationService, SystemNotificationService>();
builder.Services.AddScoped<IAccountGroupService, AccountGroupService>();
builder.Services.AddScoped<IMessageCacheService, MessageCacheService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();

// 扩展模块系统
builder.Services.AddModuleSystem();

// WTelegram 客户端配置
builder.Services.Configure<WTelegramRuntimeOptions>(o =>
{
    o.IdleTtl = TimeSpan.FromMinutes(5);
    o.ReapInterval = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<WTelegramClientManager>();
builder.Services.AddSingleton<ITdClientManager>(sp => sp.GetRequiredService<WTelegramClientManager>());
builder.Services.AddHostedService<WTelegramClientReaperHostedService>();

// 健康检查服务（同时注册为 HostedService 和接口）
builder.Services.AddSingleton<AccountHealthCheckService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AccountHealthCheckService>());
builder.Services.AddSingleton<IAccountHealthCheckService>(sp => sp.GetRequiredService<AccountHealthCheckService>());

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseStaticFiles();

static bool IsSameOrigin(HttpContext ctx)
{
    if (ctx.Request.Host.HasValue == false)
        return false;

    var expectedOrigin = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";

    var origin = ctx.Request.Headers.Origin.ToString();
    if (!string.IsNullOrWhiteSpace(origin))
        return string.Equals(origin, expectedOrigin, StringComparison.OrdinalIgnoreCase);

    var referer = ctx.Request.Headers.Referer.ToString();
    if (string.IsNullOrWhiteSpace(referer) || !Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        return false;

    var refererOrigin = $"{refererUri.Scheme}://{refererUri.Host}{(refererUri.IsDefaultPort ? "" : ":" + refererUri.Port)}";
    return string.Equals(refererOrigin, expectedOrigin, StringComparison.OrdinalIgnoreCase);
}

app.MapPost("/api/auth/login", async (HttpContext httpContext, AuthService authService, IAuditLogStore audit, CancellationToken ct) =>
{
    if (!IsSameOrigin(httpContext))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var form = await httpContext.Request.ReadFormAsync(ct);
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = await authService.ValidateAsync(username, password, ct);
    if (user is null)
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("用户名或密码错误"));

    await authService.SignInAsync(httpContext, user, ct);
    await audit.WriteAsync(user.Username, "auth.login", "管理员登录", ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken: ct);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/api/auth/update-credentials", async (HttpContext httpContext, IUserStore userStore, IAppConfigStore appConfigStore, PasswordHasher hasher, AuthService authService, IAuditLogStore audit, CancellationToken ct) =>
{
    if (!IsSameOrigin(httpContext))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    var userIdStr = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!long.TryParse(userIdStr, out var userId))
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("未登录或登录状态异常"));

    var form = await httpContext.Request.ReadFormAsync(ct);
    var currentPassword = form["currentPassword"].ToString();
    var newUsername = form["newUsername"].ToString();
    var newPassword = form["newPassword"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (string.IsNullOrWhiteSpace(currentPassword))
        return Results.Redirect("/security?error=" + Uri.EscapeDataString("请输入当前密码"));

    if (string.IsNullOrWhiteSpace(newPassword) && string.IsNullOrWhiteSpace(newUsername))
        return Results.Redirect("/security?error=" + Uri.EscapeDataString("请至少修改用户名或密码其中一项"));

    if (!string.IsNullOrWhiteSpace(newPassword))
    {
        if (newPassword.Length < 12)
            return Results.Redirect("/security?error=" + Uri.EscapeDataString("新密码至少 12 位（建议包含大小写字母、数字和符号）"));
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            return Results.Redirect("/security?error=" + Uri.EscapeDataString("两次输入的新密码不一致"));
    }

    var user = await userStore.FindByIdAsync(userId, ct);
    if (user is null)
        return Results.Redirect("/security?error=" + Uri.EscapeDataString("用户不存在"));

    if (!hasher.Verify(currentPassword, user.PasswordHash))
        return Results.Redirect("/security?error=" + Uri.EscapeDataString("当前密码错误"));

    var passwordHash = string.IsNullOrWhiteSpace(newPassword) ? null : hasher.Hash(newPassword);
    var usernameToUpdate = string.IsNullOrWhiteSpace(newUsername) ? null : newUsername;

    try
    {
        await userStore.UpdateCredentialsAsync(userId, usernameToUpdate, passwordHash, ct);

        // 记录审计日志
        var changes = new List<string>();
        if (!string.IsNullOrWhiteSpace(usernameToUpdate))
            changes.Add($"用户名: {user.Username} → {usernameToUpdate}");
        if (!string.IsNullOrWhiteSpace(newPassword))
            changes.Add("密码已修改");

        await audit.WriteAsync(
            user.Username,
            "admin.credentials_update",
            string.Join(", ", changes),
            ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
            cancellationToken: ct);
    }
    catch (Exception ex)
    {
        return Results.Redirect("/security?error=" + Uri.EscapeDataString(ex.Message));
    }

    // 清除"需要改密"标记（若存在）
    await appConfigStore.SetStringAsync(AppConfigKeys.SecurityMustChangeAdminCredentials, "false", ct);

    // 重新签发 Cookie，避免修改用户名后仍显示旧用户名
    var updated = await userStore.FindByIdAsync(userId, ct);
    if (updated is not null)
        await authService.SignInAsync(httpContext, updated, ct);

    return Results.Redirect("/security?success=" + Uri.EscapeDataString("已更新账号信息"));
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/api/auth/logout", async (HttpContext httpContext, IAuditLogStore audit, CancellationToken ct) =>
{
    var username = httpContext.User.Identity?.Name ?? "unknown";
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await audit.WriteAsync(username, "auth.logout", "退出登录", ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken: ct);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/api/accounts/{accountId:long}/export", async (long accountId, HttpContext httpContext, IAccountService accountService, IAuditLogStore audit, CancellationToken ct) =>
{
    var stream = await accountService.ExportZipAsync(accountId, ct);

    // 记录审计日志
    var username = httpContext.User.Identity?.Name ?? "unknown";
    await audit.WriteAsync(
        username,
        "account.export",
        $"导出账号 {accountId}",
        targetId: accountId.ToString(),
        ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
        userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
        cancellationToken: ct);

    return Results.File(stream, "application/zip", $"account-{accountId}.zip");
}).RequireAuthorization();

app.MapGet("/api/accounts/export", async (HttpContext httpContext, IAccountService accountService, IAuditLogStore audit, CancellationToken ct) =>
{
    var stream = await accountService.ExportAllZipAsync(ct);

    // 记录审计日志
    var username = httpContext.User.Identity?.Name ?? "unknown";
    await audit.WriteAsync(
        username,
        "account.export_all",
        "导出所有账号",
        ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
        userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
        cancellationToken: ct);

    return Results.File(stream, "application/zip", "accounts.zip");
}).RequireAuthorization();

app.MapPost("/api/accounts/import", async (HttpContext httpContext, IAccountService accountService, IAuditLogStore audit, CancellationToken ct) =>
{
    if (!IsSameOrigin(httpContext))
        return Results.StatusCode(StatusCodes.Status403Forbidden);

    if (!httpContext.Request.HasFormContentType)
        return Results.BadRequest("必须使用 multipart/form-data");

    var form = await httpContext.Request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.Redirect("/accounts?error=" + Uri.EscapeDataString("未选择文件"));

    await using var stream = file.OpenReadStream();
    var importedIds = await accountService.ImportZipAsync(stream, file.Length, ct);

    // 记录审计日志
    var username = httpContext.User.Identity?.Name ?? "unknown";
    await audit.WriteAsync(
        username,
        "account.import",
        $"导入 {importedIds.Count} 个账号",
        ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
        userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
        additionalData: $"AccountIds: {string.Join(",", importedIds)}",
        cancellationToken: ct);

    return Results.Redirect("/accounts");
}).RequireAuthorization().DisableAntiforgery();

app.MapDelete("/api/accounts/{accountId:long}", async (long accountId, HttpContext httpContext, IAccountService accountService, IAuditLogStore audit, CancellationToken ct) =>
{
    var username = httpContext.User.Identity?.Name ?? "unknown";

    try
    {
        await accountService.DeleteAsync(accountId, ct);

        // 记录审计日志
        await audit.WriteAsync(
            username,
            "account.delete",
            $"删除账号 {accountId}",
            targetId: accountId.ToString(),
            ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
            cancellationToken: ct);

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        // 记录失败的审计日志
        await audit.WriteAsync(
            username,
            "account.delete",
            $"删除账号 {accountId} 失败",
            targetId: accountId.ToString(),
            ipAddress: httpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: httpContext.Request.Headers["User-Agent"].ToString(),
            result: "failure",
            additionalData: ex.Message,
            cancellationToken: ct);

        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<TelegramHub>("/hubs/telegram").RequireAuthorization();
app.MapControllers();

await InitializeAsync(app);
await app.RunAsync();

static async Task InitializeAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await db.EnsureSqliteWalAsync(CancellationToken.None);

    var userStore = scope.ServiceProvider.GetRequiredService<IUserStore>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var appConfigStore = scope.ServiceProvider.GetRequiredService<IAppConfigStore>();

    var adminUser = Environment.GetEnvironmentVariable("ADMIN_INIT_USER") ?? "admin";
    var adminPass = Environment.GetEnvironmentVariable("ADMIN_INIT_PASS") ?? "tgadmin";

    // 安全检测：检查是否使用默认/弱密码
    var insecurePasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "change-me", "changeme", "tgadmin", "password", "admin", "123456", "admin123", "root", ""
    };

    if (insecurePasswords.Contains(adminPass))
    {
        logger.LogWarning(
            "════════════════════════════════════════════════════════════════════════");
        logger.LogWarning(
            "⚠️  安全警告：检测到使用不安全的管理员密码！");
        logger.LogWarning(
            "    请立即设置环境变量 ADMIN_INIT_PASS 为强密码（至少 12 位，包含大小写字母、数字和符号）");
        logger.LogWarning(
            "════════════════════════════════════════════════════════════════════════");

        logger.LogWarning("建议登录后在 Web 界面中立即修改用户名和密码。");
    }

    var hash = hasher.Hash(adminPass);
    var adminId = await userStore.EnsureAdminAsync(adminUser, hash, CancellationToken.None);

    // 若管理员仍在使用默认/弱密码，则设置“需要改密”标记；一旦用户修改密码，该标记会被清除
    var adminRecord = await userStore.FindByIdAsync(adminId, CancellationToken.None);
    var mustChange = adminRecord is not null
                     && insecurePasswords.Contains(adminPass)
                     && hasher.Verify(adminPass, adminRecord.PasswordHash);
    await appConfigStore.SetStringAsync(AppConfigKeys.SecurityMustChangeAdminCredentials, mustChange ? "true" : "false", CancellationToken.None);

    logger.LogInformation("✅ 已切换到 WTelegramClient 轻量级架构，支持 1C1G 服务器运行 50+ 账号");

    // 加载已启用的扩展模块
    var moduleManager = scope.ServiceProvider.GetRequiredService<IModuleManager>();
    try
    {
        await moduleManager.LoadEnabledModulesAsync(CancellationToken.None);
        logger.LogInformation("✅ 扩展模块系统已初始化");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ 加载扩展模块时发生错误，但应用程序将继续运行");
    }
}
