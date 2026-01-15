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

        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AccountEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<int?>("ApiIdOverride")
                .HasColumnType("INTEGER")
                .HasColumnName("api_id_override");

            b.Property<string>("DataDir")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("data_dir");

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

            b.HasKey("Id");

            b.HasIndex("Phone");

            b.ToTable("accounts", (string)null);
        });

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

        modelBuilder.Entity("TgLitePanel.Infrastructure.Persistence.Entities.AuditLogEntity", b =>
        {
            b.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER")
                .HasColumnName("id");

            b.Property<string>("Action")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("action");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("TEXT")
                .HasColumnName("created_at_utc");

            b.Property<string>("Ip")
                .HasColumnType("TEXT")
                .HasColumnName("ip");

            b.Property<string>("Summary")
                .IsRequired()
                .HasColumnType("TEXT")
                .HasColumnName("summary");

            b.Property<long?>("UserId")
                .HasColumnType("INTEGER")
                .HasColumnName("user_id");

            b.HasKey("Id");

            b.HasIndex("CreatedAtUtc");

            b.ToTable("audit_logs", (string)null);
        });

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
    }
}

