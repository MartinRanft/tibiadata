namespace TibiaDataApi.Services.Persistence
{
    public sealed class DatabaseOptions
    {
        public const string SectionName = "Database";

        public string Provider { get; set; } = DatabaseProviderNames.MariaDb;

        public string ProductionConnectionStringName { get; set; } = "DatabaseConnection";

        public string DevelopmentConnectionStringName { get; set; } = "DatabaseConnectionDev";

        public string TestConnectionStringName { get; set; } = "DatabaseConnectionTest";
    }

    public static class DatabaseProviderNames
    {
        public const string MariaDb = "MariaDb";
        public const string MySql = "MySql";
        public const string PostgreSql = "PostgreSql";
        public const string SqlServer = "SqlServer";
    }
}