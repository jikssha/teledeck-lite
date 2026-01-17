using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountDto>> ListAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<AccountDto> items, int totalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<AccountDto> GetAsync(long accountId, CancellationToken cancellationToken);

    Task<long> StartLoginAsync(string phone, CancellationToken cancellationToken);
    Task ResendCodeAsync(long accountId, CancellationToken cancellationToken);
    Task SubmitLoginCodeAsync(long accountId, string code, CancellationToken cancellationToken);
    Task SubmitLoginPasswordAsync(long accountId, string password, CancellationToken cancellationToken);

    Task<Stream> ExportZipAsync(long accountId, CancellationToken cancellationToken);
    Task<Stream> ExportAllZipAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<long>> ImportZipAsync(Stream zipStream, long zipLength, CancellationToken cancellationToken);
    Task<long> ImportSessionFileAsync(Stream sessionStream, string fileName, CancellationToken cancellationToken);
    Task<long> ImportStringSessionAsync(string sessionString, CancellationToken cancellationToken);
    Task DeleteAsync(long accountId, CancellationToken cancellationToken);
}
