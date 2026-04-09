using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using TibiaDataApi.AdminAccess;
using TibiaDataApi.Contracts.Admin;

namespace TibiaDataApi.Api.Tests
{
    public sealed class ApiSmokeTests : IClassFixture<TibiaDataApiApiFactory>
    {
        private readonly TibiaDataApiApiFactory _factory;

        public ApiSmokeTests(TibiaDataApiApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthLive_ReturnsHealthyPayload()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/health/live");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"status\":\"Healthy\"", body);
        }

        [Fact]
        public async Task PublicScalar_RendersAdminShortcut()
        {
            using HttpClient client = _factory.CreateClient();

            string body = await client.GetStringAsync("/");

            Assert.Contains(AdminAccessDefaults.PublicUiTitle, body);
            Assert.Contains("Admin", body);
            Assert.Contains(AdminAccessDefaults.LoginPath, body);
        }

        [Fact]
        public async Task LegacyScalarPath_RedirectsToRoot()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/scalar");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task AdminOpenApi_RequiresLogin_WhenAnonymous()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync(AdminAccessDefaults.AdminOpenApiPath);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal(AdminAccessDefaults.LoginPath, response.Headers.Location!.AbsolutePath);
        }

        [Fact]
        public async Task AdminDashboard_RendersLogin_WhenAnonymous()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync(AdminAccessDefaults.AdminDashboardPath);
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Admin Access", body);
            Assert.Contains("Open Admin Panel", body);
        }

        [Fact]
        public async Task AdminDashboard_RendersSetup_WhenAnonymousInProduction_AndPasswordIsNotConfigured()
        {
            const string productionTestConnectionString =
                "Server=127.0.0.1;Port=65535;Database=tibiadataapi_prod_test;User=test;Password=test;charset=utf8mb4;Allow User Variables=True;";

            string? previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
            Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", productionTestConnectionString);

            try
            {
                using WebApplicationFactory<Program> productionFactory = _factory.WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                });

                using HttpClient client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

                using HttpRequestMessage request = new(HttpMethod.Get, AdminAccessDefaults.AdminDashboardPath);
                request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
                request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "tibiadata.bytewizards.de");
                request.Headers.TryAddWithoutValidation("X-Forwarded-Port", "443");

                using HttpResponseMessage response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("Admin Setup", body);
                Assert.Contains("Save Admin Password", body);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", previousConnectionString);
            }
        }

        [Fact]
        public async Task ProductionHttpRequest_RedirectsToHttps()
        {
            const string productionTestConnectionString =
                "Server=127.0.0.1;Port=65535;Database=tibiadataapi_prod_test;User=test;Password=test;charset=utf8mb4;Allow User Variables=True;";

            string? previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
            Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", productionTestConnectionString);

            try
            {
                using WebApplicationFactory<Program> productionFactory = _factory.WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                });

                using HttpClient client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

                using HttpResponseMessage response = await client.GetAsync("/");

                Assert.Equal(HttpStatusCode.PermanentRedirect, response.StatusCode);
                Assert.NotNull(response.Headers.Location);
                Assert.Equal("https", response.Headers.Location!.Scheme);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", previousConnectionString);
            }
        }

        [Fact]
        public async Task ProductionForwardedHttpsRequest_DoesNotRedirect()
        {
            const string productionTestConnectionString =
                "Server=127.0.0.1;Port=65535;Database=tibiadataapi_prod_test;User=test;Password=test;charset=utf8mb4;Allow User Variables=True;";

            string? previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
            Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", productionTestConnectionString);

            try
            {
                using WebApplicationFactory<Program> productionFactory = _factory.WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Production");
                });

                using HttpClient client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

                using HttpRequestMessage request = new(HttpMethod.Get, "/");
                request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
                request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "tibiadata.bytewizards.de");
                request.Headers.TryAddWithoutValidation("X-Forwarded-Port", "443");

                using HttpResponseMessage response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(AdminAccessDefaults.PublicUiTitle, body);
            }
            finally
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__DatabaseConnection", previousConnectionString);
            }
        }

        [Fact]
        public async Task AdminLogin_RejectsMissingAntiforgeryToken()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            using HttpResponseMessage response = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath
                }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AdminOpenApi_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminScalarPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
            Assert.NotNull(loginResponse.Headers.Location);

            using HttpResponseMessage adminResponse = await client.GetAsync(AdminAccessDefaults.AdminOpenApiPath);
            string body = await adminResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
            Assert.Contains("\"openapi\"", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AdminDashboard_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
            Assert.NotNull(loginResponse.Headers.Location);
            Assert.Equal(AdminAccessDefaults.AdminDashboardPath, loginResponse.Headers.Location!.OriginalString);

            using HttpResponseMessage adminResponse = await client.GetAsync(AdminAccessDefaults.AdminDashboardPath);
            string body = await adminResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
            Assert.Contains("Admin Dashboard", body);
            Assert.Contains("Operations Console", body);
        }

        [Fact]
        public async Task AdminDashboard_NormalizesUnsafeReturnUrl()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            string body = await client.GetStringAsync($"{AdminAccessDefaults.AdminDashboardPath}?returnUrl=//evil.example");

            Assert.Contains($"name=\"returnUrl\" value=\"{AdminAccessDefaults.AdminDashboardPath}\"", body);
        }

        [Fact]
        public async Task AdminDashboard_EmitsSecurityHeaders()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync(AdminAccessDefaults.AdminDashboardPath);

            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        }

        [Fact]
        public async Task MetricsEndpoint_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage metricsResponse = await client.GetAsync("/metrics");
            string body = await metricsResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
            Assert.Contains("http_requests_received_total", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AdminMetricsOverviewEndpoint_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage metricsResponse = await client.GetAsync("/api/admin/system/metrics");
            string body = await metricsResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
            Assert.Contains("\"metricFamilyCount\":", body, StringComparison.Ordinal);
            Assert.Contains("\"rawMetricsText\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AdminDatabaseLoadEndpoint_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage response = await client.GetAsync("/api/admin/system/database-load");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"totalCommands\":", body, StringComparison.Ordinal);
            Assert.Contains("\"windowMinutes\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AdminProtectionRulesEndpoint_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage response = await client.GetAsync("/api/admin/security/protection-rules");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"rules\":", body, StringComparison.Ordinal);
            Assert.Contains("\"rateLimitPolicies\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AdminRateLimitSettingsEndpoint_IsAccessibleAfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage response = await client.GetAsync("/api/admin/security/rate-limit-settings");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"enabled\":", body, StringComparison.Ordinal);
            Assert.Contains("\"policies\":", body, StringComparison.Ordinal);
            Assert.Contains("\"version\":", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AdminRateLimitSettingsUpdateEndpoint_PersistsChanges_AfterSuccessfulLogin()
        {
            using TibiaDataApiApiFactory isolatedFactory = new();
            using HttpClient client = isolatedFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            AdminRateLimitSettingsResponse? currentSettings =
                await client.GetFromJsonAsync<AdminRateLimitSettingsResponse>("/api/admin/security/rate-limit-settings");

            Assert.NotNull(currentSettings);
            Assert.NotEmpty(currentSettings!.Policies);

            AdminConfigurableRateLimitPolicyItem publicApiPolicy = currentSettings.Policies
                                                                                   .Single(policy => policy.ScopeKey == "public-api");

            AdminUpdateRateLimitSettingsRequest request = new(
                currentSettings.Enabled,
                currentSettings.Policies
                               .Select(policy => new AdminUpdateRateLimitPolicyItem(
                                   policy.ScopeKey,
                                   policy.ScopeKey == "public-api" ? publicApiPolicy.TokenLimit + 1 : policy.TokenLimit,
                                   policy.TokensPerPeriod,
                                   policy.ReplenishmentSeconds,
                                   policy.TokenQueueLimit,
                                   policy.ConcurrentPermitLimit,
                                   policy.ConcurrentQueueLimit))
                               .ToList());

            using HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
                "/api/admin/security/rate-limit-settings",
                request);

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            AdminRateLimitSettingsResponse? updatedSettings =
                await updateResponse.Content.ReadFromJsonAsync<AdminRateLimitSettingsResponse>();

            Assert.NotNull(updatedSettings);
            Assert.True(updatedSettings!.Version >= currentSettings.Version);
            Assert.Equal(publicApiPolicy.TokenLimit + 1,
                updatedSettings.Policies.Single(policy => policy.ScopeKey == "public-api").TokenLimit);

            AdminRateLimitSettingsResponse? reloadedSettings =
                await client.GetFromJsonAsync<AdminRateLimitSettingsResponse>("/api/admin/security/rate-limit-settings");

            Assert.NotNull(reloadedSettings);
            Assert.Equal(publicApiPolicy.TokenLimit + 1,
                reloadedSettings!.Policies.Single(policy => policy.ScopeKey == "public-api").TokenLimit);
        }

        [Fact]
        public async Task AdminRunScraper_InvalidTriggeredByLength_ReturnsBadRequest_InsteadOfServerError()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/admin/scraper/run",
                new
                {
                    triggeredBy = new string('x', 101)
                });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BestiaryFilteredCreatures_WithoutQueryParameters_ReturnsOk()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/api/v1/bestiary/creatures");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task BestiaryFilteredCreatures_WithInvalidSort_ReturnsBadRequest()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/api/v1/bestiary/creatures?sort=invalid");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AdminApiStatsEndpoint_ReturnsExtendedAnalyticsPayload_AfterSuccessfulLogin()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });

            string antiforgeryToken = await GetAntiforgeryTokenAsync(client);

            using HttpResponseMessage loginResponse = await client.PostAsync(
                AdminAccessDefaults.LoginPath,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["password"] = AdminAccessDefaults.DevelopmentPassword,
                    ["returnUrl"] = AdminAccessDefaults.AdminDashboardPath,
                    [AdminAccessDefaults.AntiforgeryFormFieldName] = antiforgeryToken
                }));

            Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

            using HttpResponseMessage statsResponse = await client.GetAsync("/api/admin/stats/api?days=1");
            string body = await statsResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
            Assert.Contains("\"blockedCount\":", body, StringComparison.Ordinal);
            Assert.Contains("\"averageResponseSizeBytes\":", body, StringComparison.Ordinal);
            Assert.Contains("\"topStatusCodes\":", body, StringComparison.Ordinal);
        }

        private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
        {
            string body = await client.GetStringAsync(AdminAccessDefaults.AdminDashboardPath);
            Match match = Regex.Match(
                body,
                $"name=\"{Regex.Escape(AdminAccessDefaults.AntiforgeryFormFieldName)}\" value=\"([^\"]+)\"",
                RegexOptions.CultureInvariant);

            Assert.True(match.Success);
            return WebUtility.HtmlDecode(match.Groups[1].Value);
        }
    }
}
