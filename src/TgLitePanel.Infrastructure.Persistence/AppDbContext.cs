using Microsoft.EntityFrameworkCore;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppConfigEntity> AppConfigs => Set<AppConfigEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<SecurityOpEntity> SecurityOps => Set<SecurityOpEntity>();
    public DbSet<SharedCodeTokenEntity> SharedCodeTokens => Set<SharedCodeTokenEntity>();

    // 新增：分组管理
    public DbSet<AccountGroupEntity> AccountGroups => Set<AccountGroupEntity>();

    // 新增：消息缓存
    public DbSet<CachedMessageEntity> CachedMessages => Set<CachedMessageEntity>();
    public DbSet<CachedChatEntity> CachedChats => Set<CachedChatEntity>();

    // 新增：Webhook 配置
    public DbSet<WebhookConfigEntity> WebhookConfigs => Set<WebhookConfigEntity>();

    // 新增：模块系统
    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();
    public DbSet<ModuleAuditLogEntity> ModuleAuditLogs => Set<ModuleAuditLogEntity>();

    // 新增：账号状态监控和告警
    public DbSet<AccountStatusLogEntity> AccountStatusLogs => Set<AccountStatusLogEntity>();
    public DbSet<AlertConfigEntity> AlertConfigs => Set<AlertConfigEntity>();
    public DbSet<AlertHistoryEntity> AlertHistories => Set<AlertHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppConfigEntity>(b =>
        {
            b.ToTable("app_configs");
            b.HasKey(x => x.Key);
            b.Property(x => x.Key).HasColumnName("key");
            b.Property(x => x.Value).HasColumnName("value");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Username).HasColumnName("username");
            b.Property(x => x.PasswordHash).HasColumnName("password_hash");
            b.Property(x => x.Role).HasColumnName("role");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<AccountEntity>(b =>
        {
            b.ToTable("accounts");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Phone).HasColumnName("phone");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.DataDir).HasColumnName("data_dir");
            b.Property(x => x.ApiIdOverride).HasColumnName("api_id_override");
            b.Property(x => x.SystemChatId).HasColumnName("system_chat_id");
            b.Property(x => x.GroupId).HasColumnName("group_id");
            b.Property(x => x.LastOnlineUtc).HasColumnName("last_online_utc");
            b.Property(x => x.LastCheckedUtc).HasColumnName("last_checked_utc");
            b.Property(x => x.LastError).HasColumnName("last_error");
            b.Property(x => x.IsOnline).HasColumnName("is_online");
            b.Property(x => x.DisplayName).HasColumnName("display_name");
            b.Property(x => x.Username).HasColumnName("username");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.Phone);
            b.HasIndex(x => x.GroupId);
        });

        modelBuilder.Entity<AuditLogEntity>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.Action).HasColumnName("action");
            b.Property(x => x.Summary).HasColumnName("summary");
            b.Property(x => x.Ip).HasColumnName("ip");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<SecurityOpEntity>(b =>
        {
            b.ToTable("security_ops");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Kind).HasColumnName("kind");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.Total).HasColumnName("total");
            b.Property(x => x.Processed).HasColumnName("processed");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<SharedCodeTokenEntity>(b =>
        {
            b.ToTable("shared_code_tokens");
            b.HasKey(x => x.Token);
            b.Property(x => x.Token).HasColumnName("token");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.Code).HasColumnName("code");
            b.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.ExpiresAtUtc);
        });

        // 账号分组配置
        modelBuilder.Entity<AccountGroupEntity>(b =>
        {
            b.ToTable("account_groups");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            b.Property(x => x.Color).HasColumnName("color").HasMaxLength(20);
            b.Property(x => x.SortOrder).HasColumnName("sort_order");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.Name).IsUnique();
        });

        // 消息缓存配置
        modelBuilder.Entity<CachedMessageEntity>(b =>
        {
            b.ToTable("cached_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.ChatId).HasColumnName("chat_id");
            b.Property(x => x.MessageId).HasColumnName("message_id");
            b.Property(x => x.SenderId).HasColumnName("sender_id");
            b.Property(x => x.SenderName).HasColumnName("sender_name").HasMaxLength(200);
            b.Property(x => x.Content).HasColumnName("content");
            b.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(50);
            b.Property(x => x.IsOutgoing).HasColumnName("is_outgoing");
            b.Property(x => x.MessageDateUtc).HasColumnName("message_date_utc");
            b.Property(x => x.CachedAtUtc).HasColumnName("cached_at_utc");
            b.HasIndex(x => new { x.AccountId, x.ChatId, x.MessageId }).IsUnique();
            b.HasIndex(x => new { x.AccountId, x.ChatId, x.MessageDateUtc });
            b.HasIndex(x => x.Content); // 用于搜索
        });

        // 聊天缓存配置
        modelBuilder.Entity<CachedChatEntity>(b =>
        {
            b.ToTable("cached_chats");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.ChatId).HasColumnName("chat_id");
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500);
            b.Property(x => x.ChatType).HasColumnName("chat_type").HasMaxLength(50);
            b.Property(x => x.UnreadCount).HasColumnName("unread_count");
            b.Property(x => x.LastMessageId).HasColumnName("last_message_id");
            b.Property(x => x.LastMessagePreview).HasColumnName("last_message_preview").HasMaxLength(500);
            b.Property(x => x.LastMessageDateUtc).HasColumnName("last_message_date_utc");
            b.Property(x => x.CachedAtUtc).HasColumnName("cached_at_utc");
            b.HasIndex(x => new { x.AccountId, x.ChatId }).IsUnique();
        });

        // Webhook 配置
        modelBuilder.Entity<WebhookConfigEntity>(b =>
        {
            b.ToTable("webhook_configs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            b.Property(x => x.Url).HasColumnName("url").HasMaxLength(2000);
            b.Property(x => x.Secret).HasColumnName("secret").HasMaxLength(500);
            b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            b.Property(x => x.Events).HasColumnName("events").HasMaxLength(500);
            b.Property(x => x.AccountIds).HasColumnName("account_ids").HasMaxLength(2000);
            b.Property(x => x.RetryCount).HasColumnName("retry_count");
            b.Property(x => x.TimeoutSeconds).HasColumnName("timeout_seconds");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.LastTriggeredAtUtc).HasColumnName("last_triggered_at_utc");
            b.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
        });

        // 模块实体配置
        modelBuilder.Entity<ModuleEntity>(b =>
        {
            b.ToTable("modules");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasMaxLength(100);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Version).HasColumnName("version").HasMaxLength(50);
            b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            b.Property(x => x.AuthorName).HasColumnName("author_name").HasMaxLength(200);
            b.Property(x => x.AuthorEmail).HasColumnName("author_email").HasMaxLength(200);
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
            b.Property(x => x.InstallPath).HasColumnName("install_path").HasMaxLength(500);
            b.Property(x => x.EntryAssembly).HasColumnName("entry_assembly").HasMaxLength(200);
            b.Property(x => x.EntryType).HasColumnName("entry_type").HasMaxLength(500);
            b.Property(x => x.InstalledAtUtc).HasColumnName("installed_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            b.Property(x => x.LastLoadedAtUtc).HasColumnName("last_loaded_at_utc");
            b.Property(x => x.InstalledBy).HasColumnName("installed_by").HasMaxLength(100);
            b.Property(x => x.Checksum).HasColumnName("checksum").HasMaxLength(128);
            b.Property(x => x.PermissionsJson).HasColumnName("permissions_json");
            b.Property(x => x.RoutesJson).HasColumnName("routes_json");
        });

        // 模块审计日志配置
        modelBuilder.Entity<ModuleAuditLogEntity>(b =>
        {
            b.ToTable("module_audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ModuleId).HasColumnName("module_id").HasMaxLength(100);
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50);
            b.Property(x => x.EventData).HasColumnName("event_data");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UserId).HasColumnName("user_id").HasMaxLength(100);
            b.HasIndex(x => x.ModuleId);
            b.HasIndex(x => x.CreatedAtUtc);
        });

        // 账号状态历史记录配置
        modelBuilder.Entity<AccountStatusLogEntity>(b =>
        {
            b.ToTable("account_status_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.IsOnline).HasColumnName("is_online");
            b.Property(x => x.Error).HasColumnName("error").HasMaxLength(2000);
            b.Property(x => x.CheckedAtUtc).HasColumnName("checked_at_utc");
            b.Property(x => x.Source).HasColumnName("source").HasMaxLength(20);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.CheckedAtUtc);
            b.HasIndex(x => new { x.AccountId, x.CheckedAtUtc });
        });

        // 告警配置
        modelBuilder.Entity<AlertConfigEntity>(b =>
        {
            b.ToTable("alert_configs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50);
            b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            b.Property(x => x.ConsecutiveFailureThreshold).HasColumnName("consecutive_failure_threshold");
            b.Property(x => x.CooldownMinutes).HasColumnName("cooldown_minutes");
            b.Property(x => x.NotifyMethods).HasColumnName("notify_methods").HasMaxLength(200);
            b.Property(x => x.AccountIdsJson).HasColumnName("account_ids_json");
            b.Property(x => x.GroupIdsJson).HasColumnName("group_ids_json");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            b.HasIndex(x => x.AlertType);
        });

        // 告警历史记录
        modelBuilder.Entity<AlertHistoryEntity>(b =>
        {
            b.ToTable("alert_histories");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.AccountId).HasColumnName("account_id");
            b.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50);
            b.Property(x => x.Message).HasColumnName("message").HasMaxLength(1000);
            b.Property(x => x.Details).HasColumnName("details");
            b.Property(x => x.NotificationSent).HasColumnName("notification_sent");
            b.Property(x => x.NotificationError).HasColumnName("notification_error").HasMaxLength(2000);
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.AlertType);
            b.HasIndex(x => x.CreatedAtUtc);
        });
    }

    public async Task EnsureSqliteWalAsync(CancellationToken cancellationToken)
    {
        await Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }
}

