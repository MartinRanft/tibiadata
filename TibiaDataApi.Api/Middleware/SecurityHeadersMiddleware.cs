namespace TibiaDataApi.Middleware
{
    public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                IHeaderDictionary headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers["Content-Security-Policy"] = "base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'";

                if(context.Request.IsHttps && !environment.IsDevelopment())
                {
                    headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }

                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}