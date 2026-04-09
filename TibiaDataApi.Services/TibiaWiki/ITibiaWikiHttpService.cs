namespace TibiaDataApi.Services.TibiaWiki
{
    public interface ITibiaWikiHttpService
    {
        Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default);
        Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default);
    }
}