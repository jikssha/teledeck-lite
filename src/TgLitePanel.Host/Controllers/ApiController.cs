using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Controllers;

/// <summary>
/// 账号管理 API
/// </summary>
[ApiController]
[Route("api/v1/accounts")]
[Authorize]
public sealed class AccountsApiController : ControllerBase
{
    private readonly IAccountStore _accountStore;
    private readonly IAccountService _accountService;
    private readonly IAccountGroupService _groupService;
    private readonly ILogger<AccountsApiController> _logger;

    public AccountsApiController(
        IAccountStore accountStore,
        IAccountService accountService,
        IAccountGroupService groupService,
        ILogger<AccountsApiController> logger)
    {
        _accountStore = accountStore;
        _accountService = accountService;
        _groupService = groupService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有账号列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts(CancellationToken ct)
    {
        var accounts = await _accountStore.ListAsync(ct);
        return Ok(new { success = true, data = accounts });
    }

    /// <summary>
    /// 获取单个账号
    /// </summary>
    [HttpGet("{accountId:long}")]
    public async Task<IActionResult> GetAccount(long accountId, CancellationToken ct)
    {
        var account = await _accountStore.GetAsync(accountId, ct);
        if (account is null)
            return NotFound(new { success = false, error = "账号不存在" });

        return Ok(new { success = true, data = account });
    }

    /// <summary>
    /// 设置账号分组
    /// </summary>
    [HttpPut("{accountId:long}/group")]
    public async Task<IActionResult> SetAccountGroup(long accountId, [FromBody] SetGroupRequest request, CancellationToken ct)
    {
        await _groupService.SetAccountGroupAsync(accountId, request.GroupId, ct);
        return Ok(new { success = true });
    }
}

public sealed class SetGroupRequest
{
    public long? GroupId { get; set; }
}

/// <summary>
/// 分组管理 API
/// </summary>
[ApiController]
[Route("api/v1/groups")]
[Authorize]
public sealed class GroupsApiController : ControllerBase
{
    private readonly IAccountGroupService _groupService;

    public GroupsApiController(IAccountGroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>
    /// 获取所有分组
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGroups(CancellationToken ct)
    {
        var groups = await _groupService.GetGroupsAsync(ct);
        return Ok(new { success = true, data = groups });
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var group = await _groupService.CreateGroupAsync(request.Name, request.Description, request.Color ?? "#1976D2", ct);
        return Ok(new { success = true, data = group });
    }

    /// <summary>
    /// 更新分组
    /// </summary>
    [HttpPut("{groupId:long}")]
    public async Task<IActionResult> UpdateGroup(long groupId, [FromBody] UpdateGroupRequest request, CancellationToken ct)
    {
        await _groupService.UpdateGroupAsync(groupId, request.Name, request.Description, request.Color ?? "#1976D2", request.SortOrder, ct);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    [HttpDelete("{groupId:long}")]
    public async Task<IActionResult> DeleteGroup(long groupId, CancellationToken ct)
    {
        await _groupService.DeleteGroupAsync(groupId, ct);
        return Ok(new { success = true });
    }
}

public sealed class CreateGroupRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
}

public sealed class UpdateGroupRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// 消息搜索 API
/// </summary>
[ApiController]
[Route("api/v1/messages")]
[Authorize]
public sealed class MessagesApiController : ControllerBase
{
    private readonly IMessageCacheService _cacheService;

    public MessagesApiController(IMessageCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <summary>
    /// 搜索消息
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages(
        [FromQuery] long accountId,
        [FromQuery] long? chatId,
        [FromQuery] string? keyword,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var request = new MessageSearchRequest
        {
            AccountId = accountId,
            ChatId = chatId,
            Keyword = keyword,
            FromDate = fromDate,
            ToDate = toDate,
            Limit = Math.Min(limit, 100),
            Offset = offset
        };

        var result = await _cacheService.SearchMessagesAsync(request, ct);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 获取聊天消息
    /// </summary>
    [HttpGet("{accountId:long}/{chatId:long}")]
    public async Task<IActionResult> GetMessages(
        long accountId,
        long chatId,
        [FromQuery] int limit = 50,
        [FromQuery] long? beforeMessageId = null,
        CancellationToken ct = default)
    {
        var messages = await _cacheService.GetMessagesAsync(accountId, chatId, Math.Min(limit, 100), beforeMessageId, ct);
        return Ok(new { success = true, data = messages });
    }
}

/// <summary>
/// Webhook 配置 API
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Authorize]
public sealed class WebhooksApiController : ControllerBase
{
    private readonly IWebhookConfigStore _store;

    public WebhooksApiController(IWebhookConfigStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 获取所有 Webhook 配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWebhooks(CancellationToken ct)
    {
        var configs = await _store.ListAsync(ct);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 创建 Webhook 配置
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateWebhook([FromBody] WebhookConfig config, CancellationToken ct)
    {
        var created = await _store.CreateAsync(config, ct);
        return Ok(new { success = true, data = created });
    }

    /// <summary>
    /// 更新 Webhook 配置
    /// </summary>
    [HttpPut("{webhookId:long}")]
    public async Task<IActionResult> UpdateWebhook(long webhookId, [FromBody] WebhookConfig config, CancellationToken ct)
    {
        config.Id = webhookId;
        await _store.UpdateAsync(config, ct);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除 Webhook 配置
    /// </summary>
    [HttpDelete("{webhookId:long}")]
    public async Task<IActionResult> DeleteWebhook(long webhookId, CancellationToken ct)
    {
        await _store.DeleteAsync(webhookId, ct);
        return Ok(new { success = true });
    }
}

/// <summary>
/// 聊天 API
/// </summary>
[ApiController]
[Route("api/v1/chats")]
[Authorize]
public sealed class ChatsApiController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatsApiController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// 获取聊天列表
    /// </summary>
    [HttpGet("{accountId:long}")]
    public async Task<IActionResult> GetChats(long accountId, CancellationToken ct)
    {
        var chats = await _chatService.GetDialogsAsync(accountId, ct);
        return Ok(new { success = true, data = chats });
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    [HttpPost("{accountId:long}/{chatId:long}/send")]
    public async Task<IActionResult> SendMessage(
        long accountId,
        long chatId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        await _chatService.SendTextAsync(accountId, chatId, request.Text, ct);
        return Ok(new { success = true });
    }
}

public sealed class SendMessageRequest
{
    public required string Text { get; set; }
}

/// <summary>
/// 健康检查 API
/// </summary>
[ApiController]
[Route("api/v1/health")]
[Authorize]
public sealed class HealthCheckApiController : ControllerBase
{
    private readonly IAccountHealthCheckService _healthCheckService;
    private readonly IAccountStatusLogStore _statusLogStore;
    private readonly IAlertConfigStore _alertConfigStore;
    private readonly IAlertHistoryStore _alertHistoryStore;
    private readonly ILogger<HealthCheckApiController> _logger;

    public HealthCheckApiController(
        IAccountHealthCheckService healthCheckService,
        IAccountStatusLogStore statusLogStore,
        IAlertConfigStore alertConfigStore,
        IAlertHistoryStore alertHistoryStore,
        ILogger<HealthCheckApiController> logger)
    {
        _healthCheckService = healthCheckService;
        _statusLogStore = statusLogStore;
        _alertConfigStore = alertConfigStore;
        _alertHistoryStore = alertHistoryStore;
        _logger = logger;
    }

    /// <summary>
    /// 触发手动健康检查
    /// </summary>
    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerHealthCheck([FromBody] TriggerHealthCheckRequest request, CancellationToken ct)
    {
        if (_healthCheckService.IsRunning)
            return Conflict(new { success = false, error = "已有检查任务正在运行" });

        var healthRequest = new HealthCheckRequest
        {
            AccountIds = request.AccountIds,
            GroupIds = request.GroupIds,
            Source = "manual"
        };

        var batchId = await _healthCheckService.TriggerCheckAsync(healthRequest, ct);
        return Ok(new { success = true, batchId });
    }

    /// <summary>
    /// 获取当前检查进度
    /// </summary>
    [HttpGet("progress")]
    public IActionResult GetProgress()
    {
        var progress = _healthCheckService.GetCurrentProgress();
        return Ok(new { success = true, data = progress, isRunning = _healthCheckService.IsRunning });
    }

    /// <summary>
    /// 获取账号状态历史
    /// </summary>
    [HttpGet("status/{accountId:long}")]
    public async Task<IActionResult> GetStatusHistory(long accountId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var history = await _statusLogStore.GetByAccountAsync(accountId, Math.Min(limit, 200), ct);
        return Ok(new { success = true, data = history });
    }

    /// <summary>
    /// 获取所有账号最新状态
    /// </summary>
    [HttpGet("status/latest")]
    public async Task<IActionResult> GetLatestStatus(CancellationToken ct)
    {
        var latest = await _statusLogStore.GetLatestForAllAsync(ct);
        return Ok(new { success = true, data = latest });
    }

    /// <summary>
    /// 获取告警配置列表
    /// </summary>
    [HttpGet("alerts/config")]
    public async Task<IActionResult> GetAlertConfigs(CancellationToken ct)
    {
        var configs = await _alertConfigStore.ListAsync(ct);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 创建告警配置
    /// </summary>
    [HttpPost("alerts/config")]
    public async Task<IActionResult> CreateAlertConfig([FromBody] AlertConfig config, CancellationToken ct)
    {
        var id = await _alertConfigStore.SaveAsync(config, ct);
        return Ok(new { success = true, id });
    }

    /// <summary>
    /// 更新告警配置
    /// </summary>
    [HttpPut("alerts/config/{configId:long}")]
    public async Task<IActionResult> UpdateAlertConfig(long configId, [FromBody] AlertConfig config, CancellationToken ct)
    {
        config = config with { Id = configId };
        await _alertConfigStore.SaveAsync(config, ct);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除告警配置
    /// </summary>
    [HttpDelete("alerts/config/{configId:long}")]
    public async Task<IActionResult> DeleteAlertConfig(long configId, CancellationToken ct)
    {
        await _alertConfigStore.DeleteAsync(configId, ct);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取告警历史
    /// </summary>
    [HttpGet("alerts/history")]
    public async Task<IActionResult> GetAlertHistory(
        [FromQuery] int limit = 50,
        [FromQuery] long? accountId = null,
        [FromQuery] string? alertType = null,
        CancellationToken ct = default)
    {
        var history = await _alertHistoryStore.ListAsync(Math.Min(limit, 200), accountId, alertType, ct);
        return Ok(new { success = true, data = history });
    }
}

public sealed class TriggerHealthCheckRequest
{
    public long[]? AccountIds { get; set; }
    public long[]? GroupIds { get; set; }
}
