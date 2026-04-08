using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Persistence;

namespace TibiaDataApi.AdminAccess
{
    internal static class AdminRecoveryConsole
    {
        private static readonly string[] RecoveryCommand = ["admin", "reset-password"];

        public static bool IsRecoveryCommand(string[] args)
        {
            return args.Length >= 2 &&
                   string.Equals(args[0], RecoveryCommand[0], StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(args[1], RecoveryCommand[1], StringComparison.OrdinalIgnoreCase);
        }

        public static async Task RunAsync(
            IServiceProvider services,
            IHostEnvironment environment,
            CancellationToken cancellationToken = default)
        {
            if(environment.IsDevelopment())
            {
                Console.WriteLine($"Development mode uses the fixed admin password '{AdminAccessDefaults.DevelopmentPassword}'.");
                return;
            }

            await using AsyncServiceScope scope = services.CreateAsyncScope();
            ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                  .CreateLogger("AdminRecoveryConsole");
            TibiaDbContext dbContext = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
            IAdminCredentialService adminCredentialService = scope.ServiceProvider.GetRequiredService<IAdminCredentialService>();

            await dbContext.ApplyMigrationsAsync(logger, cancellationToken);

            string password = ReadPassword("New admin password: ");
            string confirmPassword = ReadPassword("Confirm new admin password: ");

            if(!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine("Passwords do not match.");
                return;
            }

            if(!AdminPasswordPolicy.TryValidate(password, out string errorMessage))
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine(errorMessage);
                return;
            }

            await adminCredentialService.SetPasswordAsync(password, cancellationToken);

            Console.WriteLine("Admin password updated successfully.");
        }

        private static string ReadPassword(string prompt)
        {
            Console.Write(prompt);

            if(Console.IsInputRedirected)
            {
                return Console.ReadLine() ?? string.Empty;
            }

            List<char> buffer = [];

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                if(keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return new string(buffer.ToArray());
                }

                if(keyInfo.Key == ConsoleKey.Backspace)
                {
                    if(buffer.Count == 0)
                    {
                        continue;
                    }

                    buffer.RemoveAt(buffer.Count - 1);
                    continue;
                }

                if(!char.IsControl(keyInfo.KeyChar))
                {
                    buffer.Add(keyInfo.KeyChar);
                }
            }
        }
    }
}