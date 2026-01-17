using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace TgLitePanel.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.1");

        // accounts
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AccountEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<int?>("ApiIdOverride")
                .HasColumnType("INTEGER")
                .HasColumnName("api_id_override");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("DataDir")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("data_dir");

            b.Property<string>("DisplayName")
                .HasColumnType("TEXT")
                .HasColumnName("display_name");

            b.Property<long?>("GroupId")
                .HasColumnType("INTEGER")
                .HasColumnName("group_id");

            b.Property<bool>("IsOnline")
                .HasColumnType("INTEGER")
                .HasColumnName("is_online");

            b.Property<DateTime?>("LastCheckedUtc")
                .HasColumnType("TEXT")
                .HasColumnName("last_checked_utc");

            b.Property<string>("LastError")
                .HasColumnType("TEXT")
                .HasColumnName("last_error");

            b.Property<DateTime?>("LastOnlineUtc")
                .HasColumnType("TEXT")
                .HasColumnName("last_online_utc");

            b.Property<string>("Phone")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("phone");

            b.Property<int>("Status")
                .HasColumnType("INTEGER")
                .HasColumnName("status");

            b.Property<long?>("SystemChatId")
                .HasColumnType("INTEGER")
                .HasColumnName("system_chat_id");

            b.Property<string>("TwoFactorPassword")
                .HasColumnType("TEXT")
                .HasColumnName("two_factor_password");

            b.Property<string>("Username")
                .HasColumnType("TEXT")
                .HasColumnName("username");

            b.HasKey("Id");

            b.HasIndex("GroupId");

            b.HasIndex("Phone");

            b.ToTable("accounts", (string)null);
        });

        // account_groups
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AccountGroupEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("Color")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("TEXT")
                .HasColumnName("color");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("Description")
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("description");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("name");

            b.Property<int>("SortOrder")
                .HasColumnType("INTEGER")
                .HasColumnName("sort_order");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique();

            b.ToTable("account_groups", (string)null);
        });

        // account_status_logs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AccountStatusLogEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<long>("AccountId")
                .HasColumnType("INTEGER")
                .HasColumnName("account_id");

            b.Property<DateTime>("CheckedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("checked_at_utc");

            b.Property<string>("Error")
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("error");

            b.Property<bool>("IsOnline")
                .HasColumnType("INTEGER")
                .HasColumnName("is_online");

            b.Property<string>("Source")
                .HasMaxLength(20)
                .HasColumnType("TEXT")
                .HasColumnName("source");

            b.HasKey("Id");

            b.HasIndex("AccountId");

            b.HasIndex("CheckedAtUtc");

            b.HasIndex("AccountId", "CheckedAtUtc");

            b.ToTable("account_status_logs", (string)null);
        });

        // alert_configs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AlertConfigEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("AccountIdsJson")
                .HasColumnType("TEXT")
                .HasColumnName("account_ids_json");

            b.Property<string>("AlertType")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("alert_type");

            b.Property<int>("ConsecutiveFailureThreshold")
                .HasColumnType("INTEGER")
                .HasColumnName("consecutive_failure_threshold");

            b.Property<int>("CooldownMinutes")
                .HasColumnType("INTEGER")
                .HasColumnName("cooldown_minutes");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("GroupIdsJson")
                .HasColumnType("TEXT")
                .HasColumnName("group_ids_json");

            b.Property<bool>("IsEnabled")
                .HasColumnType("INTEGER")
                .HasColumnName("is_enabled");

            b.Property<string>("NotifyMethods")
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("notify_methods");

            b.Property<DateTime?>("UpdatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("updated_at_utc");

            b.HasKey("Id");

            b.HasIndex("AlertType");

            b.ToTable("alert_configs", (string)null);
        });

        // alert_histories
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AlertHistoryEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<long>("AccountId")
                .HasColumnType("INTEGER")
                .HasColumnName("account_id");

            b.Property<string>("AlertType")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("alert_type");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("Details")
                .HasColumnType("TEXT")
                .HasColumnName("details");

            b.Property<string>("Message")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("TEXT")
                .HasColumnName("message");

            b.Property<string>("NotificationError")
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("notification_error");

            b.Property<bool>("NotificationSent")
                .HasColumnType("INTEGER")
                .HasColumnName("notification_sent");

            b.HasKey("Id");

            b.HasIndex("AccountId");

            b.HasIndex("AlertType");

            b.HasIndex("CreatedAtUtc");

            b.ToTable("alert_histories", (string)null);
        });

        // app_configs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AppConfigEntity", b =>
        {
            b.Property<string>("Key")
                .HasColumnType("TEXT")
                .HasColumnName("key");

            b.Property<DateTime>("UpdatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("updated_at_utc");

            b.Property<string>("Value")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("value");

            b.HasKey("Key");

            b.ToTable("app_configs", (string)null);
        });

        // audit_logs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AuditLogEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("Action")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("action");

            b.Property<string>("AdditionalData")
                .HasColumnType("TEXT")
                .HasColumnName("additional_data");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("TEXT")
                .HasColumnName("created_at");

            b.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("description");

            b.Property<string>("IpAddress")
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("ip_address");

            b.Property<string>("Result")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("TEXT")
                .HasColumnName("result");

            b.Property<string>("TargetId")
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("target_id");

            b.Property<string>("UserAgent")
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("user_agent");

            b.Property<string>("UserName")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("user_name");

            b.HasKey("Id");

            b.HasIndex("Action");

            b.HasIndex("CreatedAt");

            b.HasIndex("UserName");

            b.HasIndex("UserName", "CreatedAt");

            b.ToTable("audit_logs", (string)null);
        });

        // cached_chats
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.CachedChatEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<long>("AccountId")
                .HasColumnType("INTEGER")
                .HasColumnName("account_id");

            b.Property<DateTime>("CachedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("cached_at_utc");

            b.Property<long>("ChatId")
                .HasColumnType("INTEGER")
                .HasColumnName("chat_id");

            b.Property<string>("ChatType")
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("chat_type");

            b.Property<DateTime?>("LastMessageDateUtc")
                .HasColumnType("TEXT")
                .HasColumnName("last_message_date_utc");

            b.Property<long?>("LastMessageId")
                .HasColumnType("INTEGER")
                .HasColumnName("last_message_id");

            b.Property<string>("LastMessagePreview")
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("last_message_preview");

            b.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("title");

            b.Property<int>("UnreadCount")
                .HasColumnType("INTEGER")
                .HasColumnName("unread_count");

            b.HasKey("Id");

            b.HasIndex("AccountId", "ChatId")
                .IsUnique();

            b.ToTable("cached_chats", (string)null);
        });

        // cached_messages
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.CachedMessageEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<long>("AccountId")
                .HasColumnType("INTEGER")
                .HasColumnName("account_id");

            b.Property<DateTime>("CachedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("cached_at_utc");

            b.Property<long>("ChatId")
                .HasColumnType("INTEGER")
                .HasColumnName("chat_id");

            b.Property<string>("Content")
                .HasColumnType("TEXT")
                .HasColumnName("content");

            b.Property<bool>("IsOutgoing")
                .HasColumnType("INTEGER")
                .HasColumnName("is_outgoing");

            b.Property<DateTime>("MessageDateUtc")
                .HasColumnType("TEXT")
                .HasColumnName("message_date_utc");

            b.Property<long>("MessageId")
                .HasColumnType("INTEGER")
                .HasColumnName("message_id");

            b.Property<string>("MessageType")
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("message_type");

            b.Property<long>("SenderId")
                .HasColumnType("INTEGER")
                .HasColumnName("sender_id");

            b.Property<string>("SenderName")
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("sender_name");

            b.HasKey("Id");

            b.HasIndex("Content");

            b.HasIndex("AccountId", "ChatId", "MessageDateUtc");

            b.HasIndex("AccountId", "ChatId", "MessageId")
                .IsUnique();

            b.ToTable("cached_messages", (string)null);
        });

        // modules
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.ModuleEntity", b =>
        {
            b.Property<string>("Id")
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("id");

            b.Property<string>("AuthorEmail")
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("author_email");

            b.Property<string>("AuthorName")
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("author_name");

            b.Property<string>("Checksum")
                .HasMaxLength(128)
                .HasColumnType("TEXT")
                .HasColumnName("checksum");

            b.Property<string>("Description")
                .HasMaxLength(1000)
                .HasColumnType("TEXT")
                .HasColumnName("description");

            b.Property<string>("EntryAssembly")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("entry_assembly");

            b.Property<string>("EntryType")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("entry_type");

            b.Property<string>("ErrorMessage")
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("error_message");

            b.Property<string>("InstallPath")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("install_path");

            b.Property<DateTime>("InstalledAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("installed_at_utc");

            b.Property<string>("InstalledBy")
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("installed_by");

            b.Property<DateTime?>("LastLoadedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("last_loaded_at_utc");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT")
                .HasColumnName("name");

            b.Property<string>("PermissionsJson")
                .HasColumnType("TEXT")
                .HasColumnName("permissions_json");

            b.Property<string>("RoutesJson")
                .HasColumnType("TEXT")
                .HasColumnName("routes_json");

            b.Property<int>("Status")
                .HasColumnType("INTEGER")
                .HasColumnName("status");

            b.Property<DateTime?>("UpdatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("updated_at_utc");

            b.Property<string>("Version")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("version");

            b.HasKey("Id");

            b.ToTable("modules", (string)null);
        });

        // module_audit_logs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.ModuleAuditLogEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("EventData")
                .HasColumnType("TEXT")
                .HasColumnName("event_data");

            b.Property<string>("EventType")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("TEXT")
                .HasColumnName("event_type");

            b.Property<string>("ModuleId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("module_id");

            b.Property<string>("UserId")
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("user_id");

            b.HasKey("Id");

            b.HasIndex("CreatedAtUtc");

            b.HasIndex("ModuleId");

            b.ToTable("module_audit_logs", (string)null);
        });

        // security_ops
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.SecurityOpEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("Kind")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("kind");

            b.Property<int>("Processed")
                .HasColumnType("INTEGER")
                .HasColumnName("processed");

            b.Property<string>("Status")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("status");

            b.Property<int>("Total")
                .HasColumnType("INTEGER")
                .HasColumnName("total");

            b.Property<DateTime>("UpdatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("updated_at_utc");

            b.HasKey("Id");

            b.ToTable("security_ops", (string)null);
        });

        // shared_code_tokens
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.SharedCodeTokenEntity", b =>
        {
            b.Property<string>("Token")
                .HasColumnType("TEXT")
                .HasColumnName("token");

            b.Property<long>("AccountId")
                .HasColumnType("INTEGER")
                .HasColumnName("account_id");

            b.Property<string>("Code")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("code");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<DateTime>("ExpiresAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("expires_at_utc");

            b.HasKey("Token");

            b.HasIndex("ExpiresAtUtc");

            b.ToTable("shared_code_tokens", (string)null);
        });

        // users
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.UserEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("PasswordHash")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("password_hash");

            b.Property<string>("Role")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("role");

            b.Property<string>("Username")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("username");

            b.HasKey("Id");

            b.HasIndex("Username")
                .IsUnique();

            b.ToTable("users", (string)null);
        });

        // webhook_configs
        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.WebhookConfigEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("AccountIds")
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("account_ids");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("Events")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("events");

            b.Property<bool>("IsEnabled")
                .HasColumnType("INTEGER")
                .HasColumnName("is_enabled");

            b.Property<string>("LastError")
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("last_error");

            b.Property<DateTime?>("LastTriggeredAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("last_triggered_at_utc");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT")
                .HasColumnName("name");

            b.Property<int>("RetryCount")
                .HasColumnType("INTEGER")
                .HasColumnName("retry_count");

            b.Property<string>("Secret")
                .HasMaxLength(500)
                .HasColumnType("TEXT")
                .HasColumnName("secret");

            b.Property<int>("TimeoutSeconds")
                .HasColumnType("INTEGER")
                .HasColumnName("timeout_seconds");

            b.Property<string>("Url")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("TEXT")
                .HasColumnName("url");

            b.HasKey("Id");

            b.ToTable("webhook_configs", (string)null);
        });
    }
}
