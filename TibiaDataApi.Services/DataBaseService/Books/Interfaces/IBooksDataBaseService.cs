using TibiaDataApi.Contracts.Public.Books;
using TibiaDataApi.Contracts.Public.Common;

namespace TibiaDataApi.Services.DataBaseService.Books.Interfaces
{
    public interface IBooksDataBaseService
    {
        Task<List<BookListItemResponse>> GetBooksAsync(CancellationToken cancellationToken = default);
        Task<BookDetailsResponse?> GetBookDetailsByNameAsync(string bookName, CancellationToken cancellationToken = default);
        Task<BookDetailsResponse?> GetBookDetailsByIdAsync(int bookId, CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetBookSyncStatesAsync(CancellationToken cancellationToken = default);
        Task<List<SyncStateResponse>> GetBookSyncStatesByDateTimeAsync(DateTime time, CancellationToken cancellationToken = default);
    }
}