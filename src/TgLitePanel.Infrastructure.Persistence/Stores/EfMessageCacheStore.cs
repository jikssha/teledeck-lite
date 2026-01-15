using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfMessageCacheStore : IMessageCacheStore
{
    private readonly AppDbContext _db;

    public EfMessageCacheStore(AppDbContext db) => _db = db;

    public async Task CacheMessagesAsync(long accountId, long chatId, IEnumerable<CachedMessage> messages, CancellationToken ct)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return;

        var messageIds = messageList.Select(m => m.MessageId).ToHashSet();

        // 获取已存在的消息ID
        var existingIdsList = await _db.CachedMessages
            .Where(m => m.AccountId == accountId && m.ChatId == chatId && messageIds.Contains(m.MessageId))
            .Select(m => m.MessageId)
            .ToListAsync(ct);
        var existingIds = existingIdsList.ToHashSet();

        // 仅插入不存在的消息
        var newMessages = messageList
            .Where(m => !existingIds.Contains(m.MessageId))
            .Select(m => new CachedMessageEntity
            {
                AccountId = accountId,
                ChatId = chatId,
                MessageId = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.SenderName,
                Content = m.Content,
                MessageType = m.MessageType,
                IsOutgoing = m.IsOutgoing,
                MessageDateUtc = m.MessageDateUtc
            });

        _db.CachedMessages.AddRange(newMessages);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CachedMessage>> GetCachedMessagesAsync(long accountId, long chatId, int limit, long? beforeMessageId, CancellationToken ct)
    {
        var query = _db.CachedMessages.AsNoTracking()
            .Where(m => m.AccountId == accountId && m.ChatId == chatId);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(m => m.MessageId < beforeMessageId.Value);
        }

        var entities = await query
            .OrderByDescending(m => m.MessageId)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<MessageSearchResult> SearchAsync(MessageSearchRequest request, CancellationToken ct)
    {
        var query = _db.CachedMessages.AsNoTracking()
            .Where(m => m.AccountId == request.AccountId);

        if (request.ChatId.HasValue)
        {
            query = query.Where(m => m.ChatId == request.ChatId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(m => m.Content != null && m.Content.Contains(request.Keyword));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(m => m.MessageDateUtc >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(m => m.MessageDateUtc <= request.ToDate.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var messages = await query
            .OrderByDescending(m => m.MessageDateUtc)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        return new MessageSearchResult
        {
            Messages = messages.Select(MapToModel).ToList(),
            TotalCount = totalCount
        };
    }

    public async Task AddMessageAsync(CachedMessage message, CancellationToken ct)
    {
        // 检查是否已存在
        var exists = await _db.CachedMessages.AnyAsync(
            m => m.AccountId == message.AccountId && m.ChatId == message.ChatId && m.MessageId == message.MessageId, ct);

        if (exists)
            return;

        var entity = new CachedMessageEntity
        {
            AccountId = message.AccountId,
            ChatId = message.ChatId,
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            Content = message.Content,
            MessageType = message.MessageType,
            IsOutgoing = message.IsOutgoing,
            MessageDateUtc = message.MessageDateUtc
        };

        _db.CachedMessages.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ClearAccountCacheAsync(long accountId, CancellationToken ct)
    {
        await _db.CachedMessages
            .Where(m => m.AccountId == accountId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task ClearChatCacheAsync(long accountId, long chatId, CancellationToken ct)
    {
        await _db.CachedMessages
            .Where(m => m.AccountId == accountId && m.ChatId == chatId)
            .ExecuteDeleteAsync(ct);
    }

    private static CachedMessage MapToModel(CachedMessageEntity entity) => new()
    {
        Id = entity.Id,
        AccountId = entity.AccountId,
        ChatId = entity.ChatId,
        MessageId = entity.MessageId,
        SenderId = entity.SenderId,
        SenderName = entity.SenderName,
        Content = entity.Content,
        MessageType = entity.MessageType,
        IsOutgoing = entity.IsOutgoing,
        MessageDateUtc = entity.MessageDateUtc
    };
}
