using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Api.Tests
{
    public sealed class TibiaDataApiApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DatabaseConnectionDev",
                "Server=127.0.0.1;Port=65535;Database=tibiadataapi_dev_test;User=test;Password=test;charset=utf8mb4;Allow User Variables=True;");

            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<TibiaDbContext>>();
                services.RemoveAll<DbContextOptions<TibiaDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<TibiaDbContext>();

                services.AddDbContext<TibiaDbContext>(options => { options.UseSqlite(_connection); });

                using ServiceProvider provider = services.BuildServiceProvider();
                using IServiceScope scope = provider.CreateScope();
                TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
                dbContext.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if(disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
