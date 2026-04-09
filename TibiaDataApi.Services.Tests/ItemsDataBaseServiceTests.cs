using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TibiaDataApi.Contracts.Public.Common;
using TibiaDataApi.Contracts.Public.Items;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.DataBaseService.Items;
using TibiaDataApi.Services.Entities.Categories;
using TibiaDataApi.Services.Entities.Items;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class ItemsDataBaseServiceTests
    {
        [Fact]
        public async Task GetItemByNameAsync_UsesNormalizedNameLookup()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Items.Add(new Item
            {
                Name = "Yellow Skull (Item)",
                NormalizedName = "yellow skull (item)",
                ActualName = "yellow skull",
                NormalizedActualName = "yellow skull",
                LastUpdated = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            ItemDetailsResponse? item = await service.GetItemByNameAsync(" yellow_skull_(item) ");

            Assert.NotNull(item);
            Assert.Equal("Yellow Skull (Item)", item!.Name);
        }

        [Fact]
        public async Task GetItemByNameAsync_ReturnsNull_WhenActualNameIsAmbiguous()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            dbContext.Items.AddRange(
                new Item
                {
                    Name = "Golden Rune Emblem (Avalanche)",
                    NormalizedName = "golden rune emblem (avalanche)",
                    ActualName = "golden rune emblem",
                    NormalizedActualName = "golden rune emblem",
                    LastUpdated = DateTime.UtcNow
                },
                new Item
                {
                    Name = "Golden Rune Emblem (Fireball)",
                    NormalizedName = "golden rune emblem (fireball)",
                    ActualName = "golden rune emblem",
                    NormalizedActualName = "golden rune emblem",
                    LastUpdated = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            ItemDetailsResponse? item = await service.GetItemByNameAsync("Golden Rune Emblem");

            Assert.Null(item);
        }

        [Fact]
        public async Task GetItemsAsync_ReturnsExpectedPage()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            WikiCategory category = new()
            {
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.WikiCategories.Add(category);
            await dbContext.SaveChangesAsync();

            dbContext.Items.AddRange(
                CreateItem("Axe", category.Id),
                CreateItem("Bow", category.Id),
                CreateItem("Club", category.Id));

            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            PagedResponse<ItemListItemResponse> page = await service.GetItemsAsync(2, 1);

            Assert.Equal(2, page.Page);
            Assert.Equal(1, page.PageSize);
            Assert.Equal(3, page.TotalCount);
            Assert.Single(page.Items);
            Assert.Equal("Bow", page.Items[0].Name);
        }

        [Fact]
        public async Task GetItemCategoriesAsync_ReturnsActiveItemCategorySlugs_WithVisibleItems()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            WikiCategory rods = new()
            {
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                SortOrder = 2,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            WikiCategory swords = new()
            {
                Slug = "swords",
                Name = "Swords",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Swords",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            WikiCategory quests = new()
            {
                Slug = "quest-overview-pages",
                Name = "Quest Overview Pages",
                ContentType = WikiContentType.Quest,
                GroupSlug = "quests",
                GroupName = "Quests",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Quest Overview Pages",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.WikiCategories.AddRange(rods, swords, quests);
            await dbContext.SaveChangesAsync();

            dbContext.Items.AddRange(
                CreateItem("Axe", swords.Id),
                CreateItem("Rod", rods.Id),
                new Item
                {
                    Name = "Hidden Sword",
                    NormalizedName = "hidden sword",
                    CategoryId = swords.Id,
                    IsMissingFromSource = true,
                    LastUpdated = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            List<string> categories = await service.GetItemCategoriesAsync();

            Assert.Equal(["swords", "rods"], categories);
        }

        [Fact]
        public async Task GetItemsByCategoryAsync_ReturnsPagedItems_ForRequestedSlug()
        {
            await using TibiaDbContext dbContext = CreateDbContext();

            WikiCategory rods = new()
            {
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            WikiCategory swords = new()
            {
                Slug = "swords",
                Name = "Swords",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Swords",
                SortOrder = 2,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.WikiCategories.AddRange(rods, swords);
            await dbContext.SaveChangesAsync();

            dbContext.Items.AddRange(
                CreateItem("Amber Rod", rods.Id),
                CreateItem("Brook Rod", rods.Id),
                CreateItem("Crystal Sword", swords.Id));

            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            List<ItemListItemResponse> items = await service.GetItemsByCategoryAsync(" RODS ", 2, 1);

            Assert.Single(items);
            Assert.Equal("Brook Rod", items[0].Name);
            Assert.Equal("rods", items[0].CategorySlug);
        }

        [Fact]
        public async Task GetItemsAsync_TranslatesProjection_WhenUsingRelationalProvider()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateSqliteDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            WikiCategory category = new()
            {
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.WikiCategories.Add(category);
            await dbContext.SaveChangesAsync();

            dbContext.Items.AddRange(
                CreateItem("Amber Rod", category.Id),
                CreateItem("Brook Rod", category.Id));
            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            PagedResponse<ItemListItemResponse> page = await service.GetItemsAsync(1, 10);

            Assert.Equal(2, page.TotalCount);
            Assert.Equal(["Amber Rod", "Brook Rod"], page.Items.Select(x => x.Name).ToArray());
        }

        [Fact]
        public async Task GetItemsByCategoryAsync_TranslatesProjection_WhenUsingRelationalProvider()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateSqliteDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            WikiCategory category = new()
            {
                Slug = "rods",
                Name = "Rods",
                ContentType = WikiContentType.Item,
                GroupSlug = "weapons",
                GroupName = "Weapons",
                SourceKind = WikiCategorySourceKind.CategoryMembers,
                SourceTitle = "Category:Rods",
                SortOrder = 1,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.WikiCategories.Add(category);
            await dbContext.SaveChangesAsync();

            dbContext.Items.AddRange(
                CreateItem("Amber Rod", category.Id),
                CreateItem("Brook Rod", category.Id));
            await dbContext.SaveChangesAsync();

            ItemsDataBaseService service = CreateService(dbContext);

            List<ItemListItemResponse> items = await service.GetItemsByCategoryAsync("rods", 1, 10);

            Assert.Equal(["Amber Rod", "Brook Rod"], items.Select(x => x.Name).ToArray());
        }

        private static TibiaDbContext CreateDbContext()
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static TibiaDbContext CreateSqliteDbContext(SqliteConnection connection)
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseSqlite(connection)
                                                       .Options;

            return new TibiaDbContext(options);
        }

        private static ItemsDataBaseService CreateService(TibiaDbContext dbContext)
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddHybridCache();
            services.AddSingleton(new CachingOptions());

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return ActivatorUtilities.CreateInstance<ItemsDataBaseService>(serviceProvider, dbContext);
        }

        private static Item CreateItem(string name, int? categoryId = null)
        {
            return new Item
            {
                Name = name,
                NormalizedName = name.ToLowerInvariant(),
                CategoryId = categoryId,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}