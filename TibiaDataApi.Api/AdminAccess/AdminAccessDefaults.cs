namespace TibiaDataApi.AdminAccess
{
    public static class AdminAccessDefaults
    {
        public const string BrandName = "TibiaData";
        public const string PublicUiTitle = BrandName;
        public const string AdminUiTitle = $"{BrandName} Admin";
        public const string AdminDashboardTitle = $"{BrandName} Admin Dashboard";
        public const string AdminOperationsTitle = $"{BrandName} Operations Console";
        public const string CookieScheme = "AdminCookie";
        public const string PolicyName = "AdminOnly";
        public const string ClaimType = "tibiadataapi.admin";
        public const string ClaimValue = "true";
        public const string DevelopmentPassword = "TibiaDataApiDev!";
        public const string AdminDashboardPath = "/admin";
        public const string LoginPath = AdminDashboardPath;
        public const string LegacyLoginPath = "/admin/login";
        public const string SetupPath = "/admin/setup";
        public const string LogoutPath = "/admin/logout";
        public const string AdminScalarPath = "/scalar/admin";
        public const string AdminOpenApiPath = "/openapi/admin.json";
        public const string PublicDocumentName = "public";
        public const string AdminDocumentName = "admin";
        public const string DefaultCookieName = "TibiaData.Admin";
        public const string DefaultAntiforgeryCookieName = "TibiaData.Admin.Antiforgery";
        public const string AntiforgeryFormFieldName = "__RequestVerificationToken";
    }
}