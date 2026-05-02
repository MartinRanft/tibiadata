using System.Net;

using Microsoft.Extensions.Logging;

namespace TibiaDataApi.Services.TibiaWiki
{
    public sealed class TibiaWikiHttpService(
        IHttpClientFactory httpClientFactory,
        TibiaWikiClientOptions options,
        ILogger<TibiaWikiHttpService> logger) : ITibiaWikiHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<TibiaWikiHttpService> _logger = logger;
        private readonly TibiaWikiClientOptions _options = options;

        public Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                requestUri,
                async (client, token) =>
                {
                    using HttpResponseMessage response = await client.GetAsync(requestUri, token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                },
                cancellationToken);
        }

        public Task<byte[]> GetBytesAsync(string requestUri, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(
                requestUri,
                async (client, token) =>
                {
                    using HttpResponseMessage response = await client.GetAsync(requestUri, token).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                },
                cancellationToken);
        }

        private async Task<T> ExecuteAsync<T>(
            string requestUri,
            Func<HttpClient, CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            int attempts = Math.Max(1, _options.MaxRetryAttempts);
            Exception? lastException = null;

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using CancellationTokenSource timeoutTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds)));

                    HttpClient client = _httpClientFactory.CreateClient("TibiaWiki");
                    return await operation(client, timeoutTokenSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = new TimeoutException(
                        $"The TibiaWiki request timed out after {_options.RequestTimeoutSeconds} seconds: {requestUri}",
                        ex);
                }
                catch (HttpRequestException ex) when (IsTransient(ex))
                {
                    lastException = ex;
                }

                if(lastException is null)
                {
                    throw new InvalidOperationException("Unexpected TibiaWiki request failure state.");
                }

                if(attempt >= attempts)
                {
                    throw lastException;
                }

                TimeSpan delay = CalculateDelay(attempt);

                _logger.LogWarning(
                    lastException,
                    "Transient TibiaWiki request failure for {RequestUri}. Attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs} ms.",
                    requestUri,
                    attempt,
                    attempts,
                    (int)delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            throw lastException ?? new InvalidOperationException("The TibiaWiki request failed without an exception.");
        }

        private bool IsTransient(HttpRequestException exception)
        {
            if(!exception.StatusCode.HasValue)
            {
                return true;
            }

            HttpStatusCode statusCode = exception.StatusCode.Value;
            int numericStatusCode = (int)statusCode;

            return statusCode == HttpStatusCode.RequestTimeout ||
                   statusCode == (HttpStatusCode)429 ||
                   numericStatusCode >= 500;
        }

        private TimeSpan CalculateDelay(int attempt)
        {
            int baseDelay = Math.Max(1, _options.BaseDelayMilliseconds);
            int maxDelay = Math.Max(baseDelay, _options.MaxDelayMilliseconds);
            int jitter = _options.MaxJitterMilliseconds <= 0
            ? 0
            : Random.Shared.Next(0, _options.MaxJitterMilliseconds + 1);

            double exponentialDelay = baseDelay * Math.Pow(2, Math.Max(0, attempt - 1));
            int boundedDelay = (int)Math.Min(maxDelay, exponentialDelay);

            return TimeSpan.FromMilliseconds(boundedDelay + jitter);
        }
    }
}