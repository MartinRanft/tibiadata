using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class DatabaseConfigurationTests
    {
        [Fact]
        public void Configure_PromotesUtf8ConnectionStringsToUtf8Mb4_ForMariaDb()
        {
            DbContextOptionsBuilder<TibiaDbContext> optionsBuilder = new();

            DatabaseConfiguration.Configure(
                optionsBuilder,
                "Server=127.0.0.1;Port=65535;Database=test;User=test;Password=test;charset=utf8;Allow User Variables=True;",
                "MariaDb");

            string? connectionString = optionsBuilder.Options.Extensions
                                                     .OfType<RelationalOptionsExtension>()
                                                     .SingleOrDefault()
                                                     ?.ConnectionString;

            Assert.NotNull(connectionString);
            Assert.Contains("utf8mb4", connectionString, StringComparison.OrdinalIgnoreCase);
        }
    }
}