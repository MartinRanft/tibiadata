using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.Services.Tests
{
    public sealed class AdminCredentialServiceTests
    {
        [Fact]
        public async Task TryInitializePasswordAsync_StoresHashedPassword()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminCredentialService service = new(dbContext);

            bool initialized = await service.TryInitializePasswordAsync("SuperSecret!123");

            string? storedHash = await dbContext.AdminCredentials
                                                .AsNoTracking()
                                                .Select(entry => entry.PasswordHash)
                                                .SingleOrDefaultAsync();

            Assert.True(initialized);
            Assert.True(await service.HasConfiguredPasswordAsync());
            Assert.NotNull(storedHash);
            Assert.NotEqual("SuperSecret!123", storedHash);
            Assert.StartsWith("pbkdf2-sha256$", storedHash, StringComparison.Ordinal);
        }

        [Fact]
        public async Task VerifyPasswordAsync_ReturnsTrueOnlyForMatchingPassword()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminCredentialService service = new(dbContext);
            await service.TryInitializePasswordAsync("TibiaDataApi!Secure");

            bool correctPasswordMatches = await service.VerifyPasswordAsync("TibiaDataApi!Secure");
            bool wrongPasswordMatches = await service.VerifyPasswordAsync("WrongPassword");

            Assert.True(correctPasswordMatches);
            Assert.False(wrongPasswordMatches);
        }

        [Fact]
        public async Task TryInitializePasswordAsync_ReturnsFalse_WhenPasswordAlreadyExists()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminCredentialService service = new(dbContext);

            bool firstInitialization = await service.TryInitializePasswordAsync("InitialPassword");
            bool secondInitialization = await service.TryInitializePasswordAsync("OtherPassword");

            int credentialCount = await dbContext.AdminCredentials.CountAsync();

            Assert.True(firstInitialization);
            Assert.False(secondInitialization);
            Assert.Equal(1, credentialCount);
        }

        [Fact]
        public async Task SetPasswordAsync_ReplacesExistingPasswordHash()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            await using TibiaDbContext dbContext = CreateDbContext(connection);
            await dbContext.Database.EnsureCreatedAsync();

            AdminCredentialService service = new(dbContext);
            await service.TryInitializePasswordAsync("InitialPass22");

            await service.SetPasswordAsync("UpdatedPass44");

            bool oldPasswordMatches = await service.VerifyPasswordAsync("InitialPass22");
            bool newPasswordMatches = await service.VerifyPasswordAsync("UpdatedPass44");

            Assert.False(oldPasswordMatches);
            Assert.True(newPasswordMatches);
        }

        [Theory]
        [InlineData("Short122Aa", false)]
        [InlineData("lowercase22", false)]
        [InlineData("UPPERCASE22", false)]
        [InlineData("NoDigitsHere", false)]
        [InlineData("StrongPass22", true)]
        [InlineData("StrongPassWord22", true)]
        public void AdminPasswordPolicy_ValidatesExpectedPasswords(string password, bool expectedValidity)
        {
            bool isValid = AdminPasswordPolicy.TryValidate(password, out _);

            Assert.Equal(expectedValidity, isValid);
        }

        private static TibiaDbContext CreateDbContext(SqliteConnection connection)
        {
            DbContextOptions<TibiaDbContext> options = new DbContextOptionsBuilder<TibiaDbContext>()
                                                       .UseSqlite(connection)
                                                       .Options;

            return new TibiaDbContext(options);
        }
    }
}
