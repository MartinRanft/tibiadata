namespace TibiaDataApi.RequestProtection
{
    internal enum RequestProtectionScope
    {
        None = 0,
        PublicApi = 1,
        AdminReadApi = 2,
        AdminMutationApi = 3,
        AdminLogin = 4,
        HealthApi = 5
    }
}