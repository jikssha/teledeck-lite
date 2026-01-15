namespace TgLitePanel.Core.Abstractions.TdLib;

public interface ITdClient : IAsyncDisposable
{
    ValueTask<string> ExecuteAsync(string json, TimeSpan timeout, CancellationToken cancellationToken);
}
