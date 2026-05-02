using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.RateLimiting;

namespace TibiaDataApi.RequestProtection
{
    internal static class RequestProtectionRateLimiter
    {
        public const string PublicApiPolicyName = "PublicApi";

        public static void Configure(RateLimiterOptions options, RequestProtectionOptions protectionOptions)
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                RequestProtectionClassifier.MarkBlocked(
                    context.HttpContext,
                    RequestProtectionContext.RateLimitBlockReason);

                if(context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    int retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "rate_limited",
                        message = "Too many requests. Please retry later."
                    },
                    cancellationToken);
            };

            options.AddPolicy(PublicApiPolicyName, context =>
            {
                RequestProtectionSettingsSnapshot settings = GetSettingsSnapshot(context, protectionOptions);

                if(!settings.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("request-protection-public-disabled");
                }

                return CreateTokenBucketPartition(context, RequestProtectionScope.PublicApi, settings.PublicApi, settings.Version);
            });

            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                CreateTokenBucketLimiter(protectionOptions),
                CreateConcurrencyLimiter(protectionOptions));
        }

        private static PartitionedRateLimiter<HttpContext> CreateTokenBucketLimiter(RequestProtectionOptions protectionOptions)
        {
            return PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                RequestProtectionScope scope = RequestProtectionClassifier.Classify(context);

                if(scope == RequestProtectionScope.None ||
                   (scope == RequestProtectionScope.PublicApi && HasExplicitPublicApiPolicy(context)))
                {
                    return RateLimitPartition.GetNoLimiter("request-protection-public");
                }

                RequestProtectionSettingsSnapshot settings = GetSettingsSnapshot(context, protectionOptions);
                if(!settings.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("request-protection-disabled");
                }

                return CreateTokenBucketPartition(context, scope, GetProfile(settings, scope), settings.Version);
            });
        }

        private static PartitionedRateLimiter<HttpContext> CreateConcurrencyLimiter(RequestProtectionOptions protectionOptions)
        {
            return PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                RequestProtectionScope scope = RequestProtectionClassifier.Classify(context);

                if(scope == RequestProtectionScope.None)
                {
                    return RateLimitPartition.GetNoLimiter("request-protection-concurrency-disabled");
                }

                RequestProtectionSettingsSnapshot settings = GetSettingsSnapshot(context, protectionOptions);
                if(!settings.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter("request-protection-concurrency-off");
                }

                string partitionKey = BuildPartitionKey(context, scope, settings.Version);
                RequestProtectionProfile profile = GetProfile(settings, scope);

                return RateLimitPartition.GetConcurrencyLimiter(partitionKey,
                    _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = Math.Max(1, profile.ConcurrentPermitLimit),
                        QueueLimit = Math.Max(0, profile.ConcurrentQueueLimit),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        }

        private static RequestProtectionProfile GetProfile(
            RequestProtectionSettingsSnapshot settings,
            RequestProtectionScope scope)
        {
            return scope switch
            {
                RequestProtectionScope.PublicApi => settings.PublicApi,
                RequestProtectionScope.AdminReadApi => settings.AdminReadApi,
                RequestProtectionScope.AdminMutationApi => settings.AdminMutationApi,
                RequestProtectionScope.AdminLogin => settings.AdminLogin,
                RequestProtectionScope.HealthApi => settings.HealthApi,
                _ => settings.PublicApi
            };
        }

        private static string BuildPartitionKey(HttpContext context, RequestProtectionScope scope, int version)
        {
            string ip = RequestProtectionClassifier.ResolveClientIp(context);
            return $"{scope}:v{Math.Max(1, version)}:{ip}";
        }

        private static RateLimitPartition<string> CreateTokenBucketPartition(
            HttpContext context,
            RequestProtectionScope scope,
            RequestProtectionProfile profile,
            int version)
        {
            string partitionKey = BuildPartitionKey(context, scope, version);

            return RateLimitPartition.GetTokenBucketLimiter(partitionKey,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = Math.Max(1, profile.TokenLimit),
                    TokensPerPeriod = Math.Max(1, profile.TokensPerPeriod),
                    ReplenishmentPeriod = TimeSpan.FromSeconds(Math.Max(1, profile.ReplenishmentSeconds)),
                    QueueLimit = Math.Max(0, profile.TokenQueueLimit),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        }

        private static bool HasExplicitPublicApiPolicy(HttpContext context)
        {
            EnableRateLimitingAttribute? attribute = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            return string.Equals(attribute?.PolicyName, PublicApiPolicyName, StringComparison.Ordinal);
        }

        private static RequestProtectionSettingsSnapshot GetSettingsSnapshot(
            HttpContext context,
            RequestProtectionOptions fallbackOptions)
        {
            IRequestProtectionConfigurationService? configurationService =
                context.RequestServices.GetService<IRequestProtectionConfigurationService>();

            if(configurationService is not null)
            {
                return configurationService.GetCurrentSnapshot();
            }

            return new RequestProtectionSettingsSnapshot(
                fallbackOptions.Enabled,
                1,
                DateTime.UtcNow,
                CloneProfile(fallbackOptions.PublicApi),
                CloneProfile(fallbackOptions.AdminReadApi),
                CloneProfile(fallbackOptions.AdminMutationApi),
                CloneProfile(fallbackOptions.AdminLogin),
                CloneProfile(fallbackOptions.HealthApi));
        }

        private static RequestProtectionProfile CloneProfile(RequestProtectionProfile profile)
        {
            return new RequestProtectionProfile
            {
                TokenLimit = profile.TokenLimit,
                TokensPerPeriod = profile.TokensPerPeriod,
                ReplenishmentSeconds = profile.ReplenishmentSeconds,
                TokenQueueLimit = profile.TokenQueueLimit,
                ConcurrentPermitLimit = profile.ConcurrentPermitLimit,
                ConcurrentQueueLimit = profile.ConcurrentQueueLimit
            };
        }
    }
}
