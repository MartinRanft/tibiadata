using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.AdminAccess;
using TibiaDataApi.Contracts.Admin;
using TibiaDataApi.Contracts.Public.Assets;
using TibiaDataApi.Contracts.Public.Books;
using TibiaDataApi.Contracts.Public.Buildings;
using TibiaDataApi.Contracts.Public.Categories;
using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Creatures;
using TibiaDataApi.Contracts.Public.HuntingPlaces;
using TibiaDataApi.Contracts.Public.Items;
using TibiaDataApi.Contracts.Public.LootStatistics;
using TibiaDataApi.Contracts.Public.Meta;
using TibiaDataApi.Contracts.Public.Quests;
using TibiaDataApi.Contracts.Public.Search;
using TibiaDataApi.Services.Categories;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Content;
using TibiaDataApi.Services.Entities.Creatures;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Text;

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
        public async Task Search_ReturnsMixedPublicResults()
        {
            string token = $"search-token-{Guid.NewGuid():N}";
            await SeedSearchDataAsync(token);

            using HttpClient client = _factory.CreateClient();

            SearchResponse? response = await client.GetFromJsonAsync<SearchResponse>(
                $"/api/v1/search?query={Uri.EscapeDataString(token)}&types=creatures,items,books&limit=10");

            Assert.NotNull(response);
            Assert.Equal(token, response!.Query);
            Assert.True(response.TotalCount >= 3);
            Assert.Contains(response.Items, x => x.Kind == "creatures" && x.Route == $"/api/v1/creatures/{Uri.EscapeDataString($"Ancient Dragon {token}")}");
            Assert.Contains(response.Items, x => x.Kind == "items" && x.Route == $"/api/v1/items/{Uri.EscapeDataString($"Dragon Wand {token}")}");
            Assert.Contains(response.Items, x => x.Kind == "books" && x.Route == $"/api/v1/books/{Uri.EscapeDataString($"Book of {token}")}");
        }

        [Fact]
        public async Task Search_ReturnsBadRequest_ForUnknownTypes()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/api/v1/search?query=dragon&types=unknown-type");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Unknown search types", body);
        }

        [Fact]
        public async Task Search_TotalCount_DoesNotChangeWithLimit()
        {
            string token = $"search-count-{Guid.NewGuid():N}";
            await SeedSearchDataAsync(token);

            using HttpClient client = _factory.CreateClient();

            SearchResponse? limited = await client.GetFromJsonAsync<SearchResponse>(
                $"/api/v1/search?query={Uri.EscapeDataString(token)}&limit=1");
            SearchResponse? expanded = await client.GetFromJsonAsync<SearchResponse>(
                $"/api/v1/search?query={Uri.EscapeDataString(token)}&limit=50");

            Assert.NotNull(limited);
            Assert.NotNull(expanded);
            Assert.Equal(limited!.TotalCount, expanded!.TotalCount);
            Assert.Single(limited.Items);
            Assert.True(expanded.Items.Count >= limited.Items.Count);
        }

        [Fact]
        public async Task MetaVersionEndpoint_ReturnsSchemaAndDataVersion()
        {
            using HttpClient client = _factory.CreateClient();

            ApiVersionResponse? response = await client.GetFromJsonAsync<ApiVersionResponse>("/api/v1/meta/version");

            Assert.NotNull(response);
            Assert.True(response!.SchemaVersion >= 1);
            Assert.Equal(64, response.DataVersion.Length);
            Assert.True(response.ItemCount >= 0);
            Assert.True(response.WikiArticleCount >= 0);
            Assert.True(response.CreatureCount >= 0);
            Assert.True(response.CategoryCount >= 0);
            Assert.True(response.AssetCount >= 0);
        }

        [Fact]
        public async Task MetaSnapshotEndpoint_ReturnsMirrorManifest()
        {
            using HttpClient client = _factory.CreateClient();

            ApiSnapshotResponse? response = await client.GetFromJsonAsync<ApiSnapshotResponse>("/api/v1/meta/snapshot");

            Assert.NotNull(response);
            Assert.True(response!.SchemaVersion >= 1);
            Assert.Equal(64, response.DataVersion.Length);
            Assert.Contains(response.Resources, x => x.Key == "items" && x.ListRoute == "/api/v1/items");
            Assert.Contains(response.Resources, x => x.Key == "creatures" && x.SyncRoute == "/api/v1/creatures/sync");
            Assert.Contains(response.Resources, x => x.Key == "assets" && x.RelatedRoutes.Contains("/api/v1/assets/metadata/{id}"));
        }

        [Fact]
        public async Task MetaDeltaEndpoint_ReturnsCentralizedChanges()
        {
            string token = $"delta-{Guid.NewGuid():N}";
            await SeedSearchDataAsync(token);

            using HttpClient client = _factory.CreateClient();

            string since = Uri.EscapeDataString(DateTime.UtcNow.AddMinutes(-5).ToString("O"));
            ApiDeltaFeedResponse? response = await client.GetFromJsonAsync<ApiDeltaFeedResponse>(
                $"/api/v1/meta/delta?since={since}&resources=items,creatures,books&limit=50");

            Assert.NotNull(response);
            Assert.True(response!.ReturnedCount >= 3);
            Assert.Contains(response.Changes, x => x.Resource == "creatures" && x.Identifier == $"Ancient Dragon {token}");
            Assert.Contains(response.Changes, x => x.Resource == "items" && x.Identifier == $"Dragon Wand {token}");
            Assert.Contains(response.Changes, x => x.Resource == "books" && x.Identifier == $"Book of {token}");
        }

        [Fact]
        public async Task MetaDeltaEndpoint_ReturnsBadRequest_ForUnknownResources()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            string since = Uri.EscapeDataString(DateTime.UtcNow.AddMinutes(-5).ToString("O"));
            using HttpResponseMessage response = await client.GetAsync($"/api/v1/meta/delta?since={since}&resources=items,unknown-resource");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Unknown delta resources", body);
        }

        [Fact]
        public async Task ItemsEndpoint_AppliesCombinedFilters()
        {
            string token = $"items-filter-{Guid.NewGuid():N}";
            await SeedFilteredItemsAsync(token);

            using HttpClient client = _factory.CreateClient();

            PagedResponse<ItemListItemResponse>? response = await client.GetFromJsonAsync<PagedResponse<ItemListItemResponse>>(
                $"/api/v1/items?itemName={Uri.EscapeDataString(token)}&category={Uri.EscapeDataString($"test-category-{token}")}&primaryType=shield&objectClass=armor&vocation=knight&sort=last-updated&descending=true&pageSize=10");

            Assert.NotNull(response);
            Assert.Equal(1, response!.TotalCount);
            Assert.Single(response.Items);
            Assert.Equal($"Dragon Shield {token}", response.Items[0].Name);
        }

        [Fact]
        public async Task CreaturesEndpoint_AppliesCombinedFilters()
        {
            string token = $"creatures-filter-{Guid.NewGuid():N}";
            await SeedFilteredCreaturesAsync(token);

            using HttpClient client = _factory.CreateClient();

            PagedResponse<CreatureListItemResponse>? response = await client.GetFromJsonAsync<PagedResponse<CreatureListItemResponse>>(
                $"/api/v1/creatures?creatureName={Uri.EscapeDataString(token)}&minHitpoints=900&maxHitpoints=1100&minExperience=600&maxExperience=800&sort=experience&descending=true&pageSize=10");

            Assert.NotNull(response);
            Assert.Equal(1, response!.TotalCount);
            Assert.Single(response.Items);
            Assert.Equal($"Ancient Dragon {token}", response.Items[0].Name);
        }

        [Fact]
        public async Task CreaturesEndpoint_ReturnsBadRequest_ForInvalidRanges()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/api/v1/creatures?minHitpoints=1000&maxHitpoints=100");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("minHitpoints", body);
        }

        [Fact]
        public async Task CreatureLootEndpoint_ReturnsStructuredLootByName()
        {
            string token = $"creature-loot-{Guid.NewGuid():N}";
            await SeedCreatureLootAsync(token);

            using HttpClient client = _factory.CreateClient();

            LootStatisticDetailsResponse? response = await client.GetFromJsonAsync<LootStatisticDetailsResponse>(
                $"/api/v1/creatures/{Uri.EscapeDataString($"Ancient Dragon {token}")}/loot");

            Assert.NotNull(response);
            Assert.Equal($"Ancient Dragon {token}", response!.CreatureName);
            Assert.Single(response.LootStatistics);
            Assert.Equal($"Dragon Ham {token}", response.LootStatistics[0].ItemName);
        }

        [Fact]
        public async Task CreatureLootEndpoint_ReturnsNotFound_ForUnknownCreature()
        {
            using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/api/v1/creatures/Definitely-Unknown-Creature/loot");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains("loot", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task HuntingPlaceDetails_ReturnsAreaCreatureSummaries()
        {
            string token = $"hunting-place-summary-{Guid.NewGuid():N}";
            string huntingPlaceName = $"Test Hunting Place {token}";
            await SeedHuntingPlaceAreaSummaryAsync(huntingPlaceName);

            using HttpClient client = _factory.CreateClient();

            HuntingPlaceDetailsResponse? response = await client.GetFromJsonAsync<HuntingPlaceDetailsResponse>(
                $"/api/v1/hunting-places/{Uri.EscapeDataString(huntingPlaceName)}");

            Assert.NotNull(response);
            Assert.NotNull(response!.StructuredData);
            Assert.Equal(3, response.Creatures.Count);
            Assert.Equal(2, response.StructuredData!.AreaCreatureSummaries.Count);

            HuntingPlaceAreaCreatureSummaryResponse firstArea = response.StructuredData.AreaCreatureSummaries[0];
            Assert.Equal("Floor 1 (Entrance)", firstArea.AreaName);
            Assert.Equal("Floor 1", firstArea.SectionName);
            Assert.Equal(2, firstArea.CreatureCount);
            Assert.Contains(firstArea.Creatures, x => x.Name == "Wild Warrior" && x.CreatureId is not null);
            Assert.Contains(firstArea.Creatures, x => x.Name == "Monk (Creature)" && x.CreatureId is not null);
            Assert.NotNull(firstArea.RecommendedLevels);
            Assert.Equal("100", firstArea.RecommendedLevels!.Knights);
            Assert.NotNull(firstArea.RecommendedSkills);
            Assert.NotNull(firstArea.RecommendedDefense);

            HuntingPlaceAreaCreatureSummaryResponse secondArea = response.StructuredData.AreaCreatureSummaries[1];
            Assert.Equal("Floor 2", secondArea.AreaName);
            Assert.Equal("Floor 2", secondArea.SectionName);
            Assert.Single(secondArea.Creatures);
            Assert.NotNull(secondArea.Creatures[0].CreatureId);
            Assert.Null(secondArea.RecommendedLevels);
        }

        [Fact]
        public async Task BookDetails_ReturnsStructuredPages()
        {
            string token = $"book-pages-{Guid.NewGuid():N}";
            string bookName = $"Book of {token}";
            await SeedStructuredBookAsync(bookName);

            using HttpClient client = _factory.CreateClient();

            BookDetailsResponse? response = await client.GetFromJsonAsync<BookDetailsResponse>(
                $"/api/v1/books/{Uri.EscapeDataString(bookName)}");

            Assert.NotNull(response);
            Assert.NotNull(response!.StructuredData);
            Assert.Equal(2, response.StructuredData!.Pages.Count);
            Assert.Equal(1, response.StructuredData.Pages[0].Index);
            Assert.Contains("Line One", response.StructuredData.Pages[0].Text);
            Assert.Equal("Library A", response.StructuredData.Pages[0].ReturnPage);
            Assert.Equal("Blue Book", response.StructuredData.Pages[1].BookType);
        }

        [Fact]
        public async Task QuestDetails_ReturnsStructuredRequirementsAndRewards()
        {
            string token = $"quest-structure-{Guid.NewGuid():N}";
            string questName = $"Quest of {token}";
            await SeedStructuredQuestAsync(questName);

            using HttpClient client = _factory.CreateClient();

            QuestDetailsResponse? response = await client.GetFromJsonAsync<QuestDetailsResponse>(
                $"/api/v1/quests/{Uri.EscapeDataString(questName)}");

            Assert.NotNull(response);
            Assert.NotNull(response!.StructuredData);
            Assert.Contains(response.StructuredData!.Requirements, x => x.Key == "premium" && x.Value == "yes");
            Assert.Contains(response.StructuredData.Requirements, x => x.Key == "levelrecommended" && x.Value == "80");
            Assert.Equal(3, response.StructuredData.Rewards.Count);
            Assert.Equal("Golden Armor", response.StructuredData.Rewards[0].Value);
        }

        [Fact]
        public async Task BuildingDetails_ReturnsStructuredAddressesAndCoordinates()
        {
            string token = $"building-structure-{Guid.NewGuid():N}";
            string buildingName = $"Building of {token}";
            await SeedStructuredBuildingAsync(buildingName);

            using HttpClient client = _factory.CreateClient();

            BuildingDetailsResponse? response = await client.GetFromJsonAsync<BuildingDetailsResponse>(
                $"/api/v1/buildings/{Uri.EscapeDataString(buildingName)}");

            Assert.NotNull(response);
            Assert.NotNull(response!.StructuredData);
            Assert.Equal(3, response.StructuredData!.Addresses.Count);
            Assert.Equal("First Street", response.StructuredData.Addresses[0].Street);
            Assert.Equal("Second Street", response.StructuredData.Addresses[1].Street);
            Assert.NotNull(response.StructuredData.Coordinates);
            Assert.Equal(127.177m, response.StructuredData.Coordinates!.X);
            Assert.Equal(123.170m, response.StructuredData.Coordinates.Y);
            Assert.Equal(7, response.StructuredData.Coordinates.Z);
        }

        [Fact]
        public async Task CategoriesEndpoint_ReturnsCategoriesByGroup()
        {
            string token = $"category-group-{Guid.NewGuid():N}";
            await SeedCategoryGroupAsync(token);

            using HttpClient client = _factory.CreateClient();

            List<CategoryListItemResponse>? response = await client.GetFromJsonAsync<List<CategoryListItemResponse>>(
                $"/api/v1/categories/group/{Uri.EscapeDataString($"group-{token}")}");

            Assert.NotNull(response);
            Assert.NotEmpty(response!);
            Assert.All(response!, x => Assert.Equal($"group-{token}", x.GroupSlug));
        }

        [Fact]
        public async Task AssetMetadataSearch_ReturnsMatchingAssets()
        {
            string token = $"asset-search-{Guid.NewGuid():N}";
            await SeedAssetMetadataAsync(token);

            using HttpClient client = _factory.CreateClient();

            List<AssetMetadataResponse>? response = await client.GetFromJsonAsync<List<AssetMetadataResponse>>(
                $"/api/v1/assets/metadata/search?fileName={Uri.EscapeDataString(token)}");

            Assert.NotNull(response);
            Assert.NotEmpty(response!);
            Assert.Contains(response!, x => x.FileName.Contains(token, StringComparison.OrdinalIgnoreCase));
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

        private async Task SeedSearchDataAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string creatureNormalizedName = $"ancient dragon {token}";
            if(await dbContext.Creatures.AnyAsync(x => x.NormalizedName == creatureNormalizedName))
            {
                return;
            }

            dbContext.Creatures.Add(new Creature
            {
                Name = $"Ancient Dragon {token}",
                NormalizedName = creatureNormalizedName,
                Hitpoints = 1000,
                Experience = 700,
                LastUpdated = DateTime.UtcNow
            });

            dbContext.Items.Add(new Item
            {
                Name = $"Dragon Wand {token}",
                NormalizedName = $"dragon wand {token}",
                LastUpdated = DateTime.UtcNow
            });

            dbContext.WikiArticles.Add(new WikiArticle
            {
                ContentType = WikiContentType.BookText,
                Title = $"Book of {token}",
                NormalizedTitle = $"book of {token}",
                Summary = "Search smoke test book.",
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedFilteredItemsAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string matchingName = $"Dragon Shield {token}";
            if(await dbContext.Items.AnyAsync(x => x.NormalizedName == EntityNameNormalizer.Normalize(matchingName)))
            {
                return;
            }

            WikiCategory category = new()
            {
                Slug = $"test-category-{token}",
                Name = $"Test Category {token}",
                ContentType = WikiContentType.Item,
                GroupSlug = "items",
                GroupName = "Items",
                SourceTitle = $"Test Category {token}",
                SortOrder = 1
            };

            dbContext.WikiCategories.Add(category);

            dbContext.Items.Add(new Item
            {
                Name = matchingName,
                NormalizedName = EntityNameNormalizer.Normalize(matchingName),
                PrimaryType = "Shield",
                SecondaryType = "Equipment",
                ObjectClass = "Armor",
                Vocation = "Knight",
                Category = category,
                LastUpdated = DateTime.UtcNow.AddMinutes(1)
            });

            dbContext.Items.Add(new Item
            {
                Name = $"Dragon Wand {token}",
                NormalizedName = EntityNameNormalizer.Normalize($"Dragon Wand {token}"),
                PrimaryType = "Wand",
                SecondaryType = "Magic",
                ObjectClass = "Weapon",
                Vocation = "Sorcerer",
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedFilteredCreaturesAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string matchingName = $"Ancient Dragon {token}";
            if(await dbContext.Creatures.AnyAsync(x => x.NormalizedName == EntityNameNormalizer.Normalize(matchingName)))
            {
                return;
            }

            dbContext.Creatures.Add(new Creature
            {
                Name = matchingName,
                NormalizedName = EntityNameNormalizer.Normalize(matchingName),
                Hitpoints = 1000,
                Experience = 700,
                LastUpdated = DateTime.UtcNow.AddMinutes(1)
            });

            dbContext.Creatures.Add(new Creature
            {
                Name = $"Young Dragon {token}",
                NormalizedName = EntityNameNormalizer.Normalize($"Young Dragon {token}"),
                Hitpoints = 300,
                Experience = 150,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedCreatureLootAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string matchingName = $"Ancient Dragon {token}";
            if(await dbContext.Creatures.AnyAsync(x => x.NormalizedName == EntityNameNormalizer.Normalize(matchingName)))
            {
                return;
            }

            dbContext.Creatures.Add(new Creature
            {
                Name = matchingName,
                NormalizedName = EntityNameNormalizer.Normalize(matchingName),
                Hitpoints = 1000,
                Experience = 700,
                LootStatisticsJson = $$"""
                    [
                      {
                        "ItemName": "Dragon Ham {{token}}",
                        "Chance": "12.5%",
                        "Rarity": "common",
                        "Raw": "Dragon Ham"
                      }
                    ]
                    """,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedHuntingPlaceAreaSummaryAsync(string huntingPlaceName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string normalizedName = EntityNameNormalizer.Normalize(huntingPlaceName);
            if(await dbContext.WikiArticles.AnyAsync(x => x.ContentType == WikiContentType.HuntingPlace && x.NormalizedTitle == normalizedName))
            {
                return;
            }

            dbContext.WikiArticles.Add(new WikiArticle
            {
                ContentType = WikiContentType.HuntingPlace,
                Title = huntingPlaceName,
                NormalizedTitle = normalizedName,
                Summary = "Derived hunting place summary test.",
                InfoboxJson = """
                    {
                      "name": "Test Hunting Place",
                      "city": "Edron",
                      "location": "Underground"
                    }
                    """,
                AdditionalAttributesJson = """
                    {
                      "LowerLevels": [
                        {
                          "areaname": "Floor 1 (Entrance)",
                          "lvlknights": "100",
                          "lvlpaladins": "90",
                          "lvlmages": "80",
                          "skknights": "85",
                          "skpaladins": "90",
                          "skmages": "70",
                          "defknights": "25",
                          "defpaladins": "20",
                          "defmages": "15"
                        }
                      ]
                    }
                    """,
                RawWikiText = """
                    == Creatures ==
                    === Floor 1 ===
                    {{CreatureList|type=List/Sorted|caption=Floor 1 (Entrance)
                     |Wild Warrior
                     |Monk (Creature)
                    }}

                    === Floor 2 ===
                    {{CreatureList|type=List/Sorted
                     |Demon Skeleton
                    }}
                    """,
                LastUpdated = DateTime.UtcNow
            });

            dbContext.Creatures.AddRange(
                new Creature
                {
                    Name = "Wild Warrior",
                    NormalizedName = EntityNameNormalizer.Normalize("Wild Warrior"),
                    Hitpoints = 150,
                    Experience = 60,
                    LastUpdated = DateTime.UtcNow
                },
                new Creature
                {
                    Name = "Monk",
                    NormalizedName = EntityNameNormalizer.Normalize("Monk"),
                    Hitpoints = 120,
                    Experience = 50,
                    LastUpdated = DateTime.UtcNow
                },
                new Creature
                {
                    Name = "Demon Skeleton",
                    NormalizedName = EntityNameNormalizer.Normalize("Demon Skeleton"),
                    Hitpoints = 400,
                    Experience = 220,
                    LastUpdated = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedStructuredBookAsync(string bookName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string normalizedName = EntityNameNormalizer.Normalize(bookName);
            if(await dbContext.WikiArticles.AnyAsync(x => x.ContentType == WikiContentType.BookText && x.NormalizedTitle == normalizedName))
            {
                return;
            }

            dbContext.WikiArticles.Add(new WikiArticle
            {
                ContentType = WikiContentType.BookText,
                Title = bookName,
                NormalizedTitle = normalizedName,
                InfoboxTemplate = "Infobox Book",
                InfoboxJson = """
                    {
                      "title": "Test Book",
                      "booktype": "Red Book",
                      "booktype2": "Blue Book",
                      "location": "Library Shelf",
                      "returnpage": "Library A",
                      "returnpage2": "Library B",
                      "text": "Line One<br>Line Two",
                      "text2": "Variant One<br />Variant Two"
                    }
                    """,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedStructuredQuestAsync(string questName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string normalizedName = EntityNameNormalizer.Normalize(questName);
            if(await dbContext.WikiArticles.AnyAsync(x => x.ContentType == WikiContentType.Quest && x.NormalizedTitle == normalizedName))
            {
                return;
            }

            dbContext.WikiArticles.Add(new WikiArticle
            {
                ContentType = WikiContentType.Quest,
                Title = questName,
                NormalizedTitle = normalizedName,
                InfoboxTemplate = "Infobox Quest",
                InfoboxJson = """
                    {
                      "name": "Test Quest",
                      "premium": "yes",
                      "level": "50",
                      "levelrecommended": "80",
                      "timeallocation": "2 hours",
                      "reward": "Golden Armor, 10 Crystal Coins, access to the secret room"
                    }
                    """,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedStructuredBuildingAsync(string buildingName)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            string normalizedName = EntityNameNormalizer.Normalize(buildingName);
            if(await dbContext.WikiArticles.AnyAsync(x => x.ContentType == WikiContentType.Building && x.NormalizedTitle == normalizedName))
            {
                return;
            }

            dbContext.WikiArticles.Add(new WikiArticle
            {
                ContentType = WikiContentType.Building,
                Title = buildingName,
                NormalizedTitle = normalizedName,
                InfoboxTemplate = "Infobox Building",
                InfoboxJson = """
                    {
                      "name": "Test Building",
                      "city": "Ab'Dendriel",
                      "location": "North of the depot.",
                      "street": "First Street",
                      "street2": "Second Street",
                      "street3": "Third Street",
                      "posx": "127.177",
                      "posy": "123.170",
                      "posz": "7"
                    }
                    """,
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedCategoryGroupAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            if(await dbContext.WikiCategories.AnyAsync(x => x.GroupSlug == $"group-{token}"))
            {
                return;
            }

            dbContext.WikiCategories.AddRange(
                new WikiCategory
                {
                    Slug = $"category-a-{token}",
                    Name = $"Category A {token}",
                    ContentType = WikiContentType.Item,
                    GroupSlug = $"group-{token}",
                    GroupName = $"Group {token}",
                    SourceTitle = $"Source {token}",
                    SortOrder = 1,
                    IsActive = true
                },
                new WikiCategory
                {
                    Slug = $"category-b-{token}",
                    Name = $"Category B {token}",
                    ContentType = WikiContentType.Item,
                    GroupSlug = $"group-{token}",
                    GroupName = $"Group {token}",
                    SourceTitle = $"Source {token}",
                    SortOrder = 2,
                    IsActive = true
                });

            await dbContext.SaveChangesAsync();
        }

        private async Task SeedAssetMetadataAsync(string token)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();

            if(await dbContext.Assets.AnyAsync(x => x.FileName == $"asset-{token}.webp"))
            {
                return;
            }

            dbContext.Assets.Add(new TibiaDataApi.Services.Entities.Assets.Asset
            {
                FileName = $"asset-{token}.webp",
                StorageKey = $"assets/{token}.webp",
                MimeType = "image/webp",
                SizeBytes = 128,
                Width = 32,
                Height = 32,
                ContentMd5 = "deadbeefdeadbeefdeadbeefdeadbeef"
            });

            await dbContext.SaveChangesAsync();
        }
    }
}
