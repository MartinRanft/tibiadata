using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using TibiaDataApi.Services.Entities.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.RequestProtection
{
    public interface IRequestProtectionConfigurationService
    {
        RequestProtectionSettingsSnapshot GetCurrentSnapshot();

        Task<RequestProtectionSettingsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

        Task<RequestProtectionSettingsSnapshot> UpdateAsync(
            bool enabled,
            IReadOnlyList<RequestProtectionPolicyUpdate> updates,
            CancellationToken cancellationToken = default);

        Task InitializeAsync(CancellationToken cancellationToken = default);
    }

    public sealed class RequestProtectionConfigurationService(
        IServiceScopeFactory scopeFactory,
        IOptions<RequestProtectionOptions> defaultsAccessor,
        ILogger<RequestProtectionConfigurationService> logger) : IRequestProtectionConfigurationService
    {
        private const string PrimaryConfigurationKey = "request-protection";
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private volatile RequestProtectionSettingsSnapshot? _snapshot;

        public RequestProtectionSettingsSnapshot GetCurrentSnapshot()
        {
            return _snapshot ?? CreateDefaultSnapshot();
        }

        public async Task<RequestProtectionSettingsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            await InitializeAsync(cancellationToken);
            return GetCurrentSnapshot();
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if(_snapshot is not null)
            {
                return;
            }

            await _initializationLock.WaitAsync(cancellationToken);
            try
            {
                if(_snapshot is not null)
                {
                    return;
                }

                using IServiceScope scope = scopeFactory.CreateScope();
                TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

                RequestProtectionConfiguration? configuration = await dbContext.RequestProtectionConfigurations
                                                                               .AsNoTracking()
                                                                               .SingleOrDefaultAsync(
                                                                                   entry => entry.Key == PrimaryConfigurationKey,
                                                                                   cancellationToken);

                _snapshot = configuration is null
                ? CreateDefaultSnapshot()
                : MapSnapshot(configuration);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to initialize request protection configuration. Falling back to defaults.");
                _snapshot = CreateDefaultSnapshot();
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task<RequestProtectionSettingsSnapshot> UpdateAsync(
            bool enabled,
            IReadOnlyList<RequestProtectionPolicyUpdate> updates,
            CancellationToken cancellationToken = default)
        {
            ValidateUpdates(updates);

            using IServiceScope scope = scopeFactory.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            RequestProtectionConfiguration configuration = await dbContext.RequestProtectionConfigurations
                                                                          .SingleOrDefaultAsync(
                                                                              entry => entry.Key == PrimaryConfigurationKey,
                                                                              cancellationToken)
                                                                      ?? CreateConfigurationFromDefaults();

            if(dbContext.Entry(configuration).State == EntityState.Detached)
            {
                dbContext.RequestProtectionConfigurations.Add(configuration);
            }

            configuration.Enabled = enabled;
            configuration.Version = Math.Max(1, configuration.Version + 1);
            configuration.UpdatedAt = DateTime.UtcNow;

            ApplyUpdates(configuration, updates);

            await dbContext.SaveChangesAsync(cancellationToken);

            RequestProtectionSettingsSnapshot snapshot = MapSnapshot(configuration);
            _snapshot = snapshot;
            return snapshot;
        }

        private RequestProtectionSettingsSnapshot CreateDefaultSnapshot()
        {
            RequestProtectionOptions defaults = defaultsAccessor.Value;
            return new RequestProtectionSettingsSnapshot(
                defaults.Enabled,
                1,
                DateTime.UtcNow,
                CloneProfile(defaults.PublicApi),
                CloneProfile(defaults.AdminReadApi),
                CloneProfile(defaults.AdminMutationApi),
                CloneProfile(defaults.AdminLogin),
                CloneProfile(defaults.HealthApi));
        }

        private RequestProtectionConfiguration CreateConfigurationFromDefaults()
        {
            RequestProtectionOptions defaults = defaultsAccessor.Value;

            return new RequestProtectionConfiguration
            {
                Key = PrimaryConfigurationKey,
                Enabled = defaults.Enabled,
                Version = 1,
                PublicApiTokenLimit = defaults.PublicApi.TokenLimit,
                PublicApiTokensPerPeriod = defaults.PublicApi.TokensPerPeriod,
                PublicApiReplenishmentSeconds = defaults.PublicApi.ReplenishmentSeconds,
                PublicApiTokenQueueLimit = defaults.PublicApi.TokenQueueLimit,
                PublicApiConcurrentPermitLimit = defaults.PublicApi.ConcurrentPermitLimit,
                PublicApiConcurrentQueueLimit = defaults.PublicApi.ConcurrentQueueLimit,
                AdminReadApiTokenLimit = defaults.AdminReadApi.TokenLimit,
                AdminReadApiTokensPerPeriod = defaults.AdminReadApi.TokensPerPeriod,
                AdminReadApiReplenishmentSeconds = defaults.AdminReadApi.ReplenishmentSeconds,
                AdminReadApiTokenQueueLimit = defaults.AdminReadApi.TokenQueueLimit,
                AdminReadApiConcurrentPermitLimit = defaults.AdminReadApi.ConcurrentPermitLimit,
                AdminReadApiConcurrentQueueLimit = defaults.AdminReadApi.ConcurrentQueueLimit,
                AdminMutationApiTokenLimit = defaults.AdminMutationApi.TokenLimit,
                AdminMutationApiTokensPerPeriod = defaults.AdminMutationApi.TokensPerPeriod,
                AdminMutationApiReplenishmentSeconds = defaults.AdminMutationApi.ReplenishmentSeconds,
                AdminMutationApiTokenQueueLimit = defaults.AdminMutationApi.TokenQueueLimit,
                AdminMutationApiConcurrentPermitLimit = defaults.AdminMutationApi.ConcurrentPermitLimit,
                AdminMutationApiConcurrentQueueLimit = defaults.AdminMutationApi.ConcurrentQueueLimit,
                AdminLoginTokenLimit = defaults.AdminLogin.TokenLimit,
                AdminLoginTokensPerPeriod = defaults.AdminLogin.TokensPerPeriod,
                AdminLoginReplenishmentSeconds = defaults.AdminLogin.ReplenishmentSeconds,
                AdminLoginTokenQueueLimit = defaults.AdminLogin.TokenQueueLimit,
                AdminLoginConcurrentPermitLimit = defaults.AdminLogin.ConcurrentPermitLimit,
                AdminLoginConcurrentQueueLimit = defaults.AdminLogin.ConcurrentQueueLimit,
                HealthApiTokenLimit = defaults.HealthApi.TokenLimit,
                HealthApiTokensPerPeriod = defaults.HealthApi.TokensPerPeriod,
                HealthApiReplenishmentSeconds = defaults.HealthApi.ReplenishmentSeconds,
                HealthApiTokenQueueLimit = defaults.HealthApi.TokenQueueLimit,
                HealthApiConcurrentPermitLimit = defaults.HealthApi.ConcurrentPermitLimit,
                HealthApiConcurrentQueueLimit = defaults.HealthApi.ConcurrentQueueLimit,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private static void ValidateUpdates(IReadOnlyList<RequestProtectionPolicyUpdate> updates)
        {
            if(updates.Count != RequestProtectionPolicyScopes.All.Count)
            {
                throw new ValidationException("All request protection policies must be supplied.");
            }

            string[] scopeKeys = updates.Select(entry => entry.ScopeKey).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if(scopeKeys.Length != RequestProtectionPolicyScopes.All.Count ||
               RequestProtectionPolicyScopes.All.Any(scope => !scopeKeys.Contains(scope.ScopeKey, StringComparer.OrdinalIgnoreCase)))
            {
                throw new ValidationException("The supplied request protection policy set is incomplete.");
            }
        }

        private static void ApplyUpdates(
            RequestProtectionConfiguration configuration,
            IReadOnlyList<RequestProtectionPolicyUpdate> updates)
        {
            foreach(RequestProtectionPolicyUpdate update in updates)
            {
                switch(update.ScopeKey)
                {
                    case RequestProtectionPolicyScopes.PublicApiScopeKey:
                        ApplyProfile(
                            update,
                            value => configuration.PublicApiTokenLimit = value,
                            value => configuration.PublicApiTokensPerPeriod = value,
                            value => configuration.PublicApiReplenishmentSeconds = value,
                            value => configuration.PublicApiTokenQueueLimit = value,
                            value => configuration.PublicApiConcurrentPermitLimit = value,
                            value => configuration.PublicApiConcurrentQueueLimit = value);
                        break;
                    case RequestProtectionPolicyScopes.AdminReadApiScopeKey:
                        ApplyProfile(
                            update,
                            value => configuration.AdminReadApiTokenLimit = value,
                            value => configuration.AdminReadApiTokensPerPeriod = value,
                            value => configuration.AdminReadApiReplenishmentSeconds = value,
                            value => configuration.AdminReadApiTokenQueueLimit = value,
                            value => configuration.AdminReadApiConcurrentPermitLimit = value,
                            value => configuration.AdminReadApiConcurrentQueueLimit = value);
                        break;
                    case RequestProtectionPolicyScopes.AdminMutationApiScopeKey:
                        ApplyProfile(
                            update,
                            value => configuration.AdminMutationApiTokenLimit = value,
                            value => configuration.AdminMutationApiTokensPerPeriod = value,
                            value => configuration.AdminMutationApiReplenishmentSeconds = value,
                            value => configuration.AdminMutationApiTokenQueueLimit = value,
                            value => configuration.AdminMutationApiConcurrentPermitLimit = value,
                            value => configuration.AdminMutationApiConcurrentQueueLimit = value);
                        break;
                    case RequestProtectionPolicyScopes.AdminLoginScopeKey:
                        ApplyProfile(
                            update,
                            value => configuration.AdminLoginTokenLimit = value,
                            value => configuration.AdminLoginTokensPerPeriod = value,
                            value => configuration.AdminLoginReplenishmentSeconds = value,
                            value => configuration.AdminLoginTokenQueueLimit = value,
                            value => configuration.AdminLoginConcurrentPermitLimit = value,
                            value => configuration.AdminLoginConcurrentQueueLimit = value);
                        break;
                    case RequestProtectionPolicyScopes.HealthApiScopeKey:
                        ApplyProfile(
                            update,
                            value => configuration.HealthApiTokenLimit = value,
                            value => configuration.HealthApiTokensPerPeriod = value,
                            value => configuration.HealthApiReplenishmentSeconds = value,
                            value => configuration.HealthApiTokenQueueLimit = value,
                            value => configuration.HealthApiConcurrentPermitLimit = value,
                            value => configuration.HealthApiConcurrentQueueLimit = value);
                        break;
                    default:
                        throw new ValidationException($"Unsupported request protection scope '{update.ScopeKey}'.");
                }
            }
        }

        private static void ApplyProfile(
            RequestProtectionPolicyUpdate update,
            Action<int> setTokenLimit,
            Action<int> setTokensPerPeriod,
            Action<int> setReplenishmentSeconds,
            Action<int> setTokenQueueLimit,
            Action<int> setConcurrentPermitLimit,
            Action<int> setConcurrentQueueLimit)
        {
            setTokenLimit(Math.Max(1, update.TokenLimit));
            setTokensPerPeriod(Math.Max(1, update.TokensPerPeriod));
            setReplenishmentSeconds(Math.Max(1, update.ReplenishmentSeconds));
            setTokenQueueLimit(Math.Max(0, update.TokenQueueLimit));
            setConcurrentPermitLimit(Math.Max(1, update.ConcurrentPermitLimit));
            setConcurrentQueueLimit(Math.Max(0, update.ConcurrentQueueLimit));
        }

        private static RequestProtectionSettingsSnapshot MapSnapshot(RequestProtectionConfiguration configuration)
        {
            return new RequestProtectionSettingsSnapshot(
                configuration.Enabled,
                Math.Max(1, configuration.Version),
                configuration.UpdatedAt,
                new RequestProtectionProfile
                {
                    TokenLimit = Math.Max(1, configuration.PublicApiTokenLimit),
                    TokensPerPeriod = Math.Max(1, configuration.PublicApiTokensPerPeriod),
                    ReplenishmentSeconds = Math.Max(1, configuration.PublicApiReplenishmentSeconds),
                    TokenQueueLimit = Math.Max(0, configuration.PublicApiTokenQueueLimit),
                    ConcurrentPermitLimit = Math.Max(1, configuration.PublicApiConcurrentPermitLimit),
                    ConcurrentQueueLimit = Math.Max(0, configuration.PublicApiConcurrentQueueLimit)
                },
                new RequestProtectionProfile
                {
                    TokenLimit = Math.Max(1, configuration.AdminReadApiTokenLimit),
                    TokensPerPeriod = Math.Max(1, configuration.AdminReadApiTokensPerPeriod),
                    ReplenishmentSeconds = Math.Max(1, configuration.AdminReadApiReplenishmentSeconds),
                    TokenQueueLimit = Math.Max(0, configuration.AdminReadApiTokenQueueLimit),
                    ConcurrentPermitLimit = Math.Max(1, configuration.AdminReadApiConcurrentPermitLimit),
                    ConcurrentQueueLimit = Math.Max(0, configuration.AdminReadApiConcurrentQueueLimit)
                },
                new RequestProtectionProfile
                {
                    TokenLimit = Math.Max(1, configuration.AdminMutationApiTokenLimit),
                    TokensPerPeriod = Math.Max(1, configuration.AdminMutationApiTokensPerPeriod),
                    ReplenishmentSeconds = Math.Max(1, configuration.AdminMutationApiReplenishmentSeconds),
                    TokenQueueLimit = Math.Max(0, configuration.AdminMutationApiTokenQueueLimit),
                    ConcurrentPermitLimit = Math.Max(1, configuration.AdminMutationApiConcurrentPermitLimit),
                    ConcurrentQueueLimit = Math.Max(0, configuration.AdminMutationApiConcurrentQueueLimit)
                },
                new RequestProtectionProfile
                {
                    TokenLimit = Math.Max(1, configuration.AdminLoginTokenLimit),
                    TokensPerPeriod = Math.Max(1, configuration.AdminLoginTokensPerPeriod),
                    ReplenishmentSeconds = Math.Max(1, configuration.AdminLoginReplenishmentSeconds),
                    TokenQueueLimit = Math.Max(0, configuration.AdminLoginTokenQueueLimit),
                    ConcurrentPermitLimit = Math.Max(1, configuration.AdminLoginConcurrentPermitLimit),
                    ConcurrentQueueLimit = Math.Max(0, configuration.AdminLoginConcurrentQueueLimit)
                },
                new RequestProtectionProfile
                {
                    TokenLimit = Math.Max(1, configuration.HealthApiTokenLimit),
                    TokensPerPeriod = Math.Max(1, configuration.HealthApiTokensPerPeriod),
                    ReplenishmentSeconds = Math.Max(1, configuration.HealthApiReplenishmentSeconds),
                    TokenQueueLimit = Math.Max(0, configuration.HealthApiTokenQueueLimit),
                    ConcurrentPermitLimit = Math.Max(1, configuration.HealthApiConcurrentPermitLimit),
                    ConcurrentQueueLimit = Math.Max(0, configuration.HealthApiConcurrentQueueLimit)
                });
        }

        private static RequestProtectionProfile CloneProfile(RequestProtectionProfile profile)
        {
            return new RequestProtectionProfile
            {
                TokenLimit = Math.Max(1, profile.TokenLimit),
                TokensPerPeriod = Math.Max(1, profile.TokensPerPeriod),
                ReplenishmentSeconds = Math.Max(1, profile.ReplenishmentSeconds),
                TokenQueueLimit = Math.Max(0, profile.TokenQueueLimit),
                ConcurrentPermitLimit = Math.Max(1, profile.ConcurrentPermitLimit),
                ConcurrentQueueLimit = Math.Max(0, profile.ConcurrentQueueLimit)
            };
        }
    }

    public sealed record RequestProtectionSettingsSnapshot(
        bool Enabled,
        int Version,
        DateTime UpdatedAtUtc,
        RequestProtectionProfile PublicApi,
        RequestProtectionProfile AdminReadApi,
        RequestProtectionProfile AdminMutationApi,
        RequestProtectionProfile AdminLogin,
        RequestProtectionProfile HealthApi);

    public sealed record RequestProtectionPolicyUpdate(
        string ScopeKey,
        int TokenLimit,
        int TokensPerPeriod,
        int ReplenishmentSeconds,
        int TokenQueueLimit,
        int ConcurrentPermitLimit,
        int ConcurrentQueueLimit);

    public static class RequestProtectionPolicyScopes
    {
        public const string PublicApiScopeKey = "public-api";
        public const string AdminReadApiScopeKey = "admin-read-api";
        public const string AdminMutationApiScopeKey = "admin-mutation-api";
        public const string AdminLoginScopeKey = "admin-login";
        public const string HealthApiScopeKey = "health-api";

        public static readonly IReadOnlyList<(string ScopeKey, string DisplayName)> All =
        [
            (PublicApiScopeKey, "Public API"),
            (AdminReadApiScopeKey, "Admin Read API"),
            (AdminMutationApiScopeKey, "Admin Mutation API"),
            (AdminLoginScopeKey, "Admin Login"),
            (HealthApiScopeKey, "Health API")
        ];
    }
}
