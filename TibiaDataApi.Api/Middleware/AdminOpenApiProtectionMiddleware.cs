using Microsoft.AspNetCore.Authentication;

using TibiaDataApi.AdminAccess;

namespace TibiaDataApi.Middleware
{
    public sealed class AdminOpenApiProtectionMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if(!context.Request.Path.Equals(AdminAccessDefaults.AdminOpenApiPath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if(context.User.Identity?.IsAuthenticated == true &&
               context.User.HasClaim(AdminAccessDefaults.ClaimType, AdminAccessDefaults.ClaimValue))
            {
                await _next(context);
                return;
            }

            await context.ChallengeAsync(AdminAccessDefaults.CookieScheme);
        }
    }
}