namespace TibiaDataApi.Services.Admin.Security
{
    public static class AdminPasswordPolicy
    {
        public static bool TryValidate(string? password, out string errorMessage)
        {
            errorMessage = "Admin password must be at least 12 characters long, include uppercase and lowercase letters, and contain at least 2 digits.";

            if(string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                return false;
            }

            bool hasLowercaseLetter = password.Any(char.IsLower);
            bool hasUppercaseLetter = password.Any(char.IsUpper);
            int digitCount = password.Count(char.IsDigit);

            if(!hasLowercaseLetter || !hasUppercaseLetter || digitCount < 2)
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
