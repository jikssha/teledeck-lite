using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TgLitePanel.Host.Services;

/// <summary>
/// 应用版本信息与更新检查（基于 GitHub Releases）
/// </summary>
public sealed class AppUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppUpdateService> _logger;
    private readonly AppUpdateOptions _options;
    private readonly object _lock = new();

    public event Action? Changed;

    public AppUpdateService(
        IHttpClientFactory httpClientFactory,
        IOptions<AppUpdateOptions> options,
        ILogger<AppUpdateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;

        CurrentVersion = GetCurrentVersion();
        RepositoryUrl = NormalizeRepositoryUrl(_options.RepositoryUrl);
        LatestVersionUrl = GetLatestVersionUrl(RepositoryUrl);
    }

    public string CurrentVersion { get; }
    public string? RepositoryUrl { get; }
    public string? LatestVersionUrl { get; }

    public string? LatestVersion { get; private set; }
    public bool IsUpdateAvailable { get; private set; }
    public DateTimeOffset? LastCheckedUtc { get; private set; }
    public string? LastError { get; private set; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(RepositoryUrl);

    public async Task CheckForUpdatesAsync(CancellationToken ct)
    {
        if (!TryParseGitHubRepo(RepositoryUrl, out var owner, out var repo))
        {
            SetState(latestVersion: null, isUpdateAvailable: false, lastError: "未配置或无法解析 GitHub 仓库地址");
            return;
        }

        try
        {
            var latest = await GetLatestReleaseTagAsync(owner, repo, ct);
            if (string.IsNullOrWhiteSpace(latest))
            {
                SetState(latestVersion: null, isUpdateAvailable: false, lastError: "未获取到最新版本信息");
                return;
            }

            var updateAvailable = IsNewerThanCurrent(latest, CurrentVersion);
            SetState(latestVersion: latest, isUpdateAvailable: updateAvailable, lastError: null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "检查更新失败");
            SetState(latestVersion: null, isUpdateAvailable: false, lastError: ex.Message);
        }
    }

    private void SetState(string? latestVersion, bool isUpdateAvailable, string? lastError)
    {
        var changed = false;
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            LastCheckedUtc = now;
            LastError = lastError;

            if (!string.Equals(LatestVersion, latestVersion, StringComparison.OrdinalIgnoreCase))
            {
                LatestVersion = latestVersion;
                changed = true;
            }

            if (IsUpdateAvailable != isUpdateAvailable)
            {
                IsUpdateAvailable = isUpdateAvailable;
                changed = true;
            }
        }

        if (changed)
            Changed?.Invoke();
    }

    private async Task<string?> GetLatestReleaseTagAsync(string owner, string repo, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Teledeck", CurrentVersion));
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            // 若仓库没有 release，会返回 404；此时改为读取最新 tag
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return await GetLatestTagAsync(owner, repo, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"GitHub API 返回 {(int)resp.StatusCode}: {body}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        return doc.RootElement.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
    }

    private async Task<string?> GetLatestTagAsync(string owner, string repo, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/tags?per_page=1";

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Teledeck", CurrentVersion));
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var resp = await client.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"GitHub API 返回 {(int)resp.StatusCode}: {body}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            return null;

        var first = doc.RootElement[0];
        return first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
    }

    private static string GetCurrentVersion()
    {
        var asm = Assembly.GetExecutingAssembly();

        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
            return informational.Trim();

        return asm.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static bool IsNewerThanCurrent(string latest, string current)
    {
        if (!TryParseComparableVersion(latest, out var latestV))
            return false;
        if (!TryParseComparableVersion(current, out var currentV))
            return false;
        return latestV > currentV;
    }

    private static bool TryParseComparableVersion(string text, out Version version)
    {
        version = new Version(0, 0, 0, 0);

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var v = text.Trim();
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            v = v[1..];

        // 去掉预发布/构建元信息
        v = v.Split('-', '+')[0];

        // 补齐到四段，避免 Version.CompareTo 在段数不同导致异常比较
        var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 4)
        {
            if (!Version.TryParse(v, out var parsed) || parsed is null)
                return false;
            version = parsed;
            return true;
        }

        while (parts.Length < 4)
            v += ".0";

        if (!Version.TryParse(v, out var parsed2) || parsed2 is null)
            return false;
        version = parsed2;
        return true;
    }

    private static bool TryParseGitHubRepo(string? repoUrl, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        if (string.IsNullOrWhiteSpace(repoUrl))
            return false;

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return false;

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return false;

        owner = segments[0];
        repo = segments[1];

        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repo = repo[..^4];

        return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
    }

    private static string? NormalizeRepositoryUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return null;

        // 统一去掉结尾 /
        return uri.ToString().TrimEnd('/');
    }

    private static string? GetLatestVersionUrl(string? repoUrl)
    {
        if (!TryParseGitHubRepo(repoUrl, out var owner, out var repo))
            return repoUrl;

        return $"https://github.com/{owner}/{repo}/releases/latest";
    }
}
