namespace TgLitePanel.Core.Abstractions.TdLib;

public sealed class TdClientLease : IAsyncDisposable
{
    private readonly ITdClientManager _manager;
    private readonly long _accountId;
    private int _disposed;

    public TdClientLease(ITdClientManager manager, long accountId, ITdClient client)
    {
        _manager = manager;
        _accountId = accountId;
        Client = client;
    }

    public ITdClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _manager.ReleaseAsync(_accountId, CancellationToken.None);
    }
}
