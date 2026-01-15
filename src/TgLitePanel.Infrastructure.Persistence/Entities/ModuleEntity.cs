namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 已安装模块实体
/// </summary>
public sealed class ModuleEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public int Status { get; set; }
    public string? ErrorMessage { get; set; }
    public required string InstallPath { get; set; }
    public required string EntryAssembly { get; set; }
    public required string EntryType { get; set; }
    public DateTime InstalledAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LastLoadedAtUtc { get; set; }
    public string? InstalledBy { get; set; }
    public required string Checksum { get; set; }
    public required string PermissionsJson { get; set; }
    public required string RoutesJson { get; set; }
}
