using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace TibiaDataApi.Services.Persistence
{
    public static class DatabaseMigrationRunner
    {
        private static readonly string[] ManagedTableNames = ["items", "creatures", "scrape_logs"];

        public static async Task ApplyMigrationsAsync(
            this TibiaDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseExistsAsync(dbContext, logger, cancellationToken);

            if(await ShouldInitializeFreshDatabaseFromModelAsync(dbContext, cancellationToken))
            {
                logger.LogInformation(
                    "Fresh {Provider} database detected. Creating schema directly from the EF model for compatibility validation.",
                    dbContext.Database.ProviderName ?? "unknown");

                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                return;
            }

            await BaselineExistingSchemaAsync(dbContext, logger, cancellationToken);

            List<string> pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if(pendingMigrations.Count == 0)
            {
                logger.LogInformation("Database is up to date. No pending migrations were found.");
                return;
            }

            logger.LogInformation(
                "Applying {Count} pending database migration(s): {Migrations}",
                pendingMigrations.Count,
                string.Join(", ", pendingMigrations));

            await ApplyPendingMigrationsAsync(dbContext, pendingMigrations, cancellationToken);

            logger.LogInformation("Database migrations applied successfully.");
        }

        private static async Task BaselineExistingSchemaAsync(
            TibiaDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            HashSet<string> existingTableNames = await GetExistingTableNamesAsync(dbContext, cancellationToken);

            if(existingTableNames.Count == 0 || existingTableNames.Contains("__EFMigrationsHistory"))
            {
                return;
            }

            bool hasAnyManagedTable = ManagedTableNames.Any(existingTableNames.Contains);
            bool hasAllManagedTables = ManagedTableNames.All(existingTableNames.Contains);

            if(!hasAnyManagedTable)
            {
                return;
            }

            if(!hasAllManagedTables)
            {
                throw new InvalidOperationException(
                    "Database contains a partial TibiaDataApi schema without EF migration history. Manual baseline is required before automatic migrations can continue.");
            }

            IMigrationsAssembly migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            string? baselineMigrationId = migrationsAssembly.Migrations.Keys
                                                            .OrderBy(migrationId => migrationId, StringComparer.Ordinal)
                                                            .FirstOrDefault();

            if(baselineMigrationId is null)
            {
                logger.LogWarning("Existing TibiaDataApi tables were detected, but no EF migration exists yet to create a baseline.");
                return;
            }

            IHistoryRepository historyRepository = dbContext.GetService<IHistoryRepository>();
            string createHistoryTableScript = historyRepository.GetCreateScript();
            string insertHistoryRowScript = historyRepository.GetInsertScript(
                new HistoryRow(baselineMigrationId, GetEfProductVersion()));

            logger.LogInformation(
                "Existing TibiaDataApi tables were detected without EF migration history. Recording baseline migration {MigrationId}.",
                baselineMigrationId);

            await dbContext.Database.ExecuteSqlRawAsync(createHistoryTableScript, cancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync(insertHistoryRowScript, cancellationToken);
        }

        private static async Task<HashSet<string>> GetExistingTableNamesAsync(
            TibiaDbContext dbContext,
            CancellationToken cancellationToken)
        {
            DbConnection connection = dbContext.Database.GetDbConnection();
            bool shouldCloseConnection = connection.State != ConnectionState.Open;

            if(shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                DataTable schema = connection.GetSchema("Tables");

                return schema.Rows
                             .Cast<DataRow>()
                             .Select(row => row["TABLE_NAME"]?.ToString())
                             .Where(tableName => !string.IsNullOrWhiteSpace(tableName))
                             .Select(tableName => tableName!)
                             .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                if(shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static string GetEfProductVersion()
        {
            return typeof(DbContext).Assembly
                                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                    .InformationalVersion
                                    .Split('+', 2)[0]
                   ?? typeof(DbContext).Assembly.GetName().Version?.ToString()
                   ?? "10.0.0";
        }

        private static async Task ApplyPendingMigrationsAsync(
            TibiaDbContext dbContext,
            IReadOnlyList<string> pendingMigrations,
            CancellationToken cancellationToken)
        {
            IMigrator migrator = dbContext.GetService<IMigrator>();
            string? currentMigration = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();

            foreach(string migrationId in pendingMigrations)
            {
                string script = migrator.GenerateScript(currentMigration, migrationId);
                List<string> commands = SplitSqlStatements(script);

                await using IDbContextTransaction transaction =
                await dbContext.Database.BeginTransactionAsync(cancellationToken);

                foreach(string command in commands)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                currentMigration = migrationId;
            }
        }

        private static List<string> SplitSqlStatements(string script)
        {
            List<string> statements = new();
            StringBuilder currentStatement = new();

            using StringReader reader = new(script);

            while (reader.ReadLine() is { } line)
            {
                string trimmedLine = line.Trim();

                if(string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                currentStatement.AppendLine(line);

                if(!trimmedLine.EndsWith(';'))
                {
                    continue;
                }

                string statement = currentStatement.ToString().Trim();
                currentStatement.Clear();

                statement = statement.TrimEnd(';').Trim();

                if(ShouldSkipStatement(statement))
                {
                    continue;
                }

                statements.Add(statement);
            }

            return statements;
        }

        private static bool ShouldSkipStatement(string statement)
        {
            return statement.Equals("START TRANSACTION", StringComparison.OrdinalIgnoreCase) ||
                   statement.Equals("COMMIT", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<bool> ShouldInitializeFreshDatabaseFromModelAsync(
            TibiaDbContext dbContext,
            CancellationToken cancellationToken)
        {
            string providerName = dbContext.Database.ProviderName ?? string.Empty;

            if(IsPrimaryMigrationProvider(providerName))
            {
                return false;
            }

            HashSet<string> existingTableNames = await GetExistingTableNamesAsync(dbContext, cancellationToken);

            if(existingTableNames.Count != 0)
            {
                return false;
            }

            List<string> appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
            return appliedMigrations.Count == 0;
        }

        private static bool IsPrimaryMigrationProvider(string providerName)
        {
            return providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task EnsureDatabaseExistsAsync(
            TibiaDbContext dbContext,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            string providerName = dbContext.Database.ProviderName ?? string.Empty;

            if(IsPrimaryMigrationProvider(providerName))
            {
                return;
            }

            IRelationalDatabaseCreator databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();

            if(await databaseCreator.ExistsAsync(cancellationToken))
            {
                return;
            }

            logger.LogInformation(
                "Database for provider {Provider} does not exist yet. Creating it before schema initialization.",
                providerName);

            await databaseCreator.CreateAsync(cancellationToken);
        }
    }
}
