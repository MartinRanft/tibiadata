using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Coravel;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Prometheus;

using Scalar.AspNetCore;

using TibiaDataApi;
using TibiaDataApi.AdminAccess;
using TibiaDataApi.HealthChecks;
using TibiaDataApi.Middleware;
using TibiaDataApi.OutputCaching;
using TibiaDataApi.RequestProtection;
using TibiaDataApi.Services.Admin.Security;
using TibiaDataApi.Services.Assets;
using TibiaDataApi.Services.BackgroundJobs;
using TibiaDataApi.Services.Caching;
using TibiaDataApi.Services.Extensions;
using TibiaDataApi.Services.HealthChecks;
using TibiaDataApi.Services.Persistence;
using TibiaDataApi.Services.Scraper;
using TibiaDataApi.Services.Scraper.Runtime;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

AdminAccessOptions configuredAdminAccessOptions =
builder.Configuration.GetSection(AdminAccessOptions.SectionName).Get<AdminAccessOptions>() ?? new AdminAccessOptions();

RequestProtectionOptions configuredRequestProtectionOptions =
builder.Configuration.GetSection(RequestProtectionOptions.SectionName).Get<RequestProtectionOptions>() ?? new RequestProtectionOptions();

CachingOptions configuredCachingOptions = CachingConfiguration.GetOptions(builder.Configuration);

BackgroundJobOptions configuredBackgroundJobOptions =
builder.Configuration.GetSection(BackgroundJobOptions.SectionName).Get<BackgroundJobOptions>() ?? new BackgroundJobOptions();

string? configuredRedisConnectionString = CachingConfiguration.GetRedisConnectionString(builder.Configuration, configuredCachingOptions);
string? configuredApplicationInsightsConnectionString =
builder.Configuration["ApplicationInsights:ConnectionString"]
?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

string configuredDataProtectionKeysDirectory =
builder.Configuration["DataProtection:KeysDirectory"]
?? Path.Combine(builder.Environment.ContentRootPath, "data", "dataprotection-keys");

string? configuredStaticAdminPassword = ResolveStaticAdminPassword(builder.Environment);


builder.Services.AddTibiaDataApiServices(builder.Configuration, builder.Environment);
builder.Services.AddOptions<AdminAccessOptions>()
       .Bind(builder.Configuration.GetSection(AdminAccessOptions.SectionName))
       .ValidateDataAnnotations();
builder.Services.AddSingleton<IAdminMetricsService, AdminMetricsService>();
builder.Services.AddSingleton<IRequestProtectionConfigurationService, RequestProtectionConfigurationService>();
builder.Services.Configure<RequestProtectionOptions>(builder.Configuration.GetSection(RequestProtectionOptions.SectionName));
builder.Services.Configure<CachingOptions>(builder.Configuration.GetSection(CachingOptions.SectionName));
builder.Services.Configure<BackgroundJobOptions>(builder.Configuration.GetSection(BackgroundJobOptions.SectionName));
builder.Services.AddDataProtection()
       .SetApplicationName("TibiaDataApi")
       .PersistKeysToFileSystem(new DirectoryInfo(configuredDataProtectionKeysDirectory));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
    ForwardedHeaders.XForwardedFor |
    ForwardedHeaders.XForwardedProto |
    ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = Math.Max(1, builder.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit") ?? 2);
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    if(builder.Configuration.GetValue<bool?>("ReverseProxy:TrustPrivateNetworks") ?? true)
    {
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("127.0.0.0"), 8));
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("fc00::"), 7));
        options.KnownProxies.Add(IPAddress.IPv6Loopback);
    }

    foreach(string configuredProxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
    {
        if(IPAddress.TryParse(configuredProxy, out IPAddress? proxyAddress))
        {
            options.KnownProxies.Add(proxyAddress);
        }
    }

    foreach(string configuredNetwork in builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
    {
        if(!TryParseIpNetwork(configuredNetwork, out System.Net.IPNetwork? ipNetwork) || ipNetwork is null)
        {
            continue;
        }

        System.Net.IPNetwork resolvedNetwork = ipNetwork.Value;
        options.KnownIPNetworks.Add(resolvedNetwork);
    }
});

if(!builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = builder.Configuration.GetValue<int?>("ReverseProxy:HttpsPort") ?? 443;
    });
}

if(configuredCachingOptions.UseRedisForOutputCache && !string.IsNullOrWhiteSpace(configuredRedisConnectionString))
{
    builder.Services.AddStackExchangeRedisOutputCache(options =>
    {
        options.Configuration = configuredRedisConnectionString;
        options.InstanceName = $"{configuredCachingOptions.RedisInstanceName}:output:";
    });
}

builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan =
    TimeSpan.FromSeconds(Math.Max(1, configuredCachingOptions.OutputCache.DefaultExpirationSeconds));

    options.AddPolicy(OutputCacheDefaults.PublicOpenApiPolicyName,
        policy => policy
                  .With(context => string.Equals(
                      context.HttpContext.Request.RouteValues["documentName"]?.ToString(),
                      AdminAccessDefaults.PublicDocumentName,
                      StringComparison.OrdinalIgnoreCase))
                  .Expire(TimeSpan.FromSeconds(Math.Max(1, configuredCachingOptions.OutputCache.PublicOpenApiSeconds)))
                  .Tag(OutputCacheDefaults.PublicOpenApiTag));

    options.AddPolicy(OutputCacheDefaults.PublicScalarPolicyName,
        policy => policy
                  .Expire(TimeSpan.FromSeconds(Math.Max(1, configuredCachingOptions.OutputCache.PublicScalarSeconds)))
                  .Tag(OutputCacheDefaults.PublicScalarTag));

    options.AddPolicy(OutputCacheDefaults.PublicApiPolicyName,
        policy => policy
                  .Expire(TimeSpan.FromSeconds(Math.Max(1, configuredCachingOptions.OutputCache.PublicApiSeconds)))
                  .Tag(OutputCacheDefaults.PublicApiTag));

    options.AddPolicy(OutputCacheDefaults.ReferenceDataPolicyName,
        policy => policy
                  .Expire(TimeSpan.FromSeconds(Math.Max(1, configuredCachingOptions.OutputCache.ReferenceDataSeconds)))
                  .Tag(OutputCacheDefaults.PublicApiTag)
                  .Tag(OutputCacheDefaults.ReferenceDataTag));
});

builder.Services.AddRateLimiter(options => { RequestProtectionRateLimiter.Configure(options, configuredRequestProtectionOptions); });

builder.Services.AddControllers();
builder.Services.AddAntiforgery(options =>
{
    options.FormFieldName = AdminAccessDefaults.AntiforgeryFormFieldName;
    options.Cookie.Name = AdminAccessDefaults.DefaultAntiforgeryCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

if(!string.IsNullOrWhiteSpace(configuredApplicationInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = configuredApplicationInsightsConnectionString;
    });
}

TryRegisterDotNetMetrics();

builder.Services.AddAuthentication(AdminAccessDefaults.CookieScheme)
       .AddCookie(AdminAccessDefaults.CookieScheme,
           options =>
           {
               string cookieName = string.IsNullOrWhiteSpace(configuredAdminAccessOptions.CookieName)
               ? AdminAccessDefaults.DefaultCookieName
               : configuredAdminAccessOptions.CookieName.Trim();

               options.Cookie.Name = cookieName;
               options.Cookie.HttpOnly = true;
               options.Cookie.IsEssential = true;
               options.Cookie.SameSite = SameSiteMode.Strict;
               options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
               options.SlidingExpiration = false;
               options.ExpireTimeSpan = TimeSpan.FromHours(NormalizeSessionHours(configuredAdminAccessOptions.SessionHours));
               options.Cookie.MaxAge = options.ExpireTimeSpan;
               options.LoginPath = AdminAccessDefaults.LoginPath;
               options.AccessDeniedPath = AdminAccessDefaults.LoginPath;
               options.Events = new CookieAuthenticationEvents
               {
                   OnRedirectToLogin = context =>
                   {
                       if(context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                       {
                           context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                           return Task.CompletedTask;
                       }

                       context.Response.Redirect(context.RedirectUri);
                       return Task.CompletedTask;
                   },
                   OnRedirectToAccessDenied = context =>
                   {
                       if(context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                       {
                           context.Response.StatusCode = StatusCodes.Status403Forbidden;
                           return Task.CompletedTask;
                       }

                       context.Response.Redirect(context.RedirectUri);
                       return Task.CompletedTask;
                   }
               };
           });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminAccessDefaults.PolicyName,
        policy =>
        {
            policy.AddAuthenticationSchemes(AdminAccessDefaults.CookieScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(AdminAccessDefaults.ClaimType, AdminAccessDefaults.ClaimValue);
        });
});

builder.Services.AddHealthChecks()
       .AddCheck<DatabaseReadinessHealthCheck>(
           "database",
           HealthStatus.Unhealthy,
           ["ready"])
       .AddCheck<RedisReadinessHealthCheck>(
           "redis",
           HealthStatus.Unhealthy,
           ["ready"])
       .AddCheck<ScraperRuntimeHealthCheck>(
           "scraper-runtime",
           HealthStatus.Degraded,
           ["ready"]);

builder.Services.AddOpenApi(AdminAccessDefaults.PublicDocumentName,
    options =>
    {
        options.ShouldInclude = description =>
        !string.Equals(description.GroupName, AdminAccessDefaults.AdminDocumentName, StringComparison.OrdinalIgnoreCase);
        options.AddDocumentTransformer(static (document, _, _) =>
        {
            document.Info.Title = AdminAccessDefaults.PublicUiTitle;
            document.Info.Version = "1.4.0";
            return Task.CompletedTask;
        });
    });

builder.Services.AddOpenApi(AdminAccessDefaults.AdminDocumentName,
    options =>
    {
        options.ShouldInclude = description =>
        string.Equals(description.GroupName, AdminAccessDefaults.AdminDocumentName, StringComparison.OrdinalIgnoreCase);
        options.AddDocumentTransformer(static (document, _, _) =>
        {
            document.Info.Title = AdminAccessDefaults.AdminUiTitle;
            return Task.CompletedTask;
        });
    });

WebApplication app = builder.Build();

if(AdminRecoveryConsole.IsRecoveryCommand(args))
{
    await AdminRecoveryConsole.RunAsync(app.Services, builder.Environment);
    return;
}


app.Services.UseScheduler(scheduler =>
{
    scheduler.Schedule<TibiaScraperJob>()
             .Cron(BuildMinuteCronExpression(1))
             .PreventOverlapping("TibiaDataScheduledScraperJob");

    if(configuredBackgroundJobOptions.ItemImageSync.Enabled)
    {
        scheduler.Schedule<ItemImageSyncJob>()
                 .Cron(BuildMinuteCronExpression(configuredBackgroundJobOptions.ItemImageSync.IntervalMinutes))
                 .PreventOverlapping("TibiaDataApiItemImageSyncJob");
    }

    if(configuredBackgroundJobOptions.CreatureImageSync.Enabled)
    {
        scheduler.Schedule<CreatureImageSyncJob>()
                 .Cron(BuildMinuteCronExpression(configuredBackgroundJobOptions.CreatureImageSync.IntervalMinutes))
                 .PreventOverlapping("TibiaDataApiCreatureImageSyncJob");
    }
});


using (IServiceScope scope = app.Services.CreateScope())
{
    ILogger<TibiaDataApi.Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<TibiaDataApi.Program>>();
    TibiaDbContext db = scope.ServiceProvider.GetRequiredService<TibiaDbContext>();
    WikiCategoryCatalogSynchronizer wikiCategoryCatalogSynchronizer =
    scope.ServiceProvider.GetRequiredService<WikiCategoryCatalogSynchronizer>();
    IRequestProtectionConfigurationService requestProtectionConfigurationService =
    scope.ServiceProvider.GetRequiredService<IRequestProtectionConfigurationService>();

    try
    {
        await db.ApplyMigrationsAsync(logger);
        await wikiCategoryCatalogSynchronizer.SynchronizeAsync(db, logger);
        await requestProtectionConfigurationService.InitializeAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization failed during startup. API will continue to run, but database-backed features remain unavailable.");
    }
}



app.UseForwardedHeaders();

if(!builder.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseMiddleware<AdminOpenApiProtectionMiddleware>();
app.UseHttpMetrics();

app.MapOpenApi()
   .CacheOutput(OutputCacheDefaults.PublicOpenApiPolicyName);

app.MapMetrics("/metrics")
   .RequireAuthorization(AdminAccessDefaults.PolicyName)
   .ExcludeFromDescription();

app.MapGet("/scalar",
       () => Results.LocalRedirect("/"))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapScalarApiReference("/",
       options =>
       {
           options.Title = AdminAccessDefaults.PublicUiTitle;
           options.Theme = ScalarTheme.BluePlanet;
           options.Layout = ScalarLayout.Modern;
           options.EnabledTargets =
           [
               ScalarTarget.Shell,
               ScalarTarget.CSharp,
               ScalarTarget.Java,
               ScalarTarget.Python,
               ScalarTarget.Php
           ];
           
           options.ShowSidebar = true;
           options.ShowDeveloperTools = DeveloperToolsVisibility.Never;
           options.HideClientButton = false;
           options.DocumentDownloadType = DocumentDownloadType.Both;
           options.HideDarkModeToggle = false;
           if(builder.Environment.IsDevelopment())
           {
               options.AddHeaderContent(BuildPublicScalarHeaderContent());
           }
           options.AddDocument(
               AdminAccessDefaults.PublicDocumentName,
               "Public API",
               "/openapi/public.json",
               true);
       })
   .CacheOutput(OutputCacheDefaults.PublicScalarPolicyName);

app.MapScalarApiReference(AdminAccessDefaults.AdminScalarPath,
    options =>
    {
        options.Title = AdminAccessDefaults.AdminUiTitle;
        options.Theme = ScalarTheme.BluePlanet;
        options.Layout = ScalarLayout.Modern;
        options.EnabledTargets =
        [
            ScalarTarget.Shell,
            ScalarTarget.CSharp,
            ScalarTarget.Java,
            ScalarTarget.Python,
            ScalarTarget.Php
        ];
        options.ShowSidebar = true;
        options.ShowDeveloperTools = DeveloperToolsVisibility.Never;
        options.HideClientButton = false;
        options.DocumentDownloadType = DocumentDownloadType.Both;
        options.HideDarkModeToggle = false;
        options.AddDocument(
            AdminAccessDefaults.AdminDocumentName,
            "Admin API",
            AdminAccessDefaults.AdminOpenApiPath,
            true);
    }).RequireAuthorization(AdminAccessDefaults.PolicyName);

app.UseMiddleware<IpBanMiddleware>();
app.UseRateLimiter();
app.UseMiddleware<ApiRequestStatisticsMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseOutputCache();


app.UseAuthorization();
app.UseAntiforgery();




app.MapControllers();

app.MapHealthChecks("/health/live",
       new HealthCheckOptions
       {
           Predicate = _ => false,
           ResponseWriter = HealthCheckResponseWriter.WriteAsync
       })
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapHealthChecks("/health/ready",
       new HealthCheckOptions
       {
           Predicate = registration => registration.Tags.Contains("ready"),
           ResponseWriter = HealthCheckResponseWriter.WriteAsync,
           ResultStatusCodes =
           {
               [HealthStatus.Healthy] = StatusCodes.Status200OK,
               [HealthStatus.Degraded] = StatusCodes.Status200OK,
               [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
           }
       })
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapGet(AdminAccessDefaults.AdminDashboardPath,
       async (
           HttpContext context,
           IIpBanService ipBanService,
           IAdminCredentialService adminCredentialService,
           IAntiforgery antiforgery,
           ILogger<Program> logger,
           string? returnUrl) =>
       {
           string safeReturnUrl = NormalizeReturnUrl(returnUrl);
           string clientIp = RequestProtectionClassifier.ResolveClientIp(context);

           bool isBlocked;
           try
           {
               isBlocked = await ipBanService.IsBlockedAsync(clientIp, context.RequestAborted);
           }
           catch (Exception ex)
           {
               logger.LogWarning(ex, "Admin IP ban lookup failed. Continuing without ban enforcement.");
               isBlocked = false;
           }

           if(isBlocked)
           {
               return RenderAdminLoginPage(context, antiforgery, safeReturnUrl, BuildAdminLockoutMessage(), StatusCodes.Status403Forbidden);
           }

           if(context.User.Identity?.IsAuthenticated == true)
           {
               return safeReturnUrl != AdminAccessDefaults.AdminDashboardPath
               ? Results.LocalRedirect(safeReturnUrl)
               : Results.Content(
                   AdminDashboardPageRenderer.Render(),
                   "text/html; charset=utf-8",
                   Encoding.UTF8);
           }

           if(configuredStaticAdminPassword is null)
           {
               try
               {
                   if(!await adminCredentialService.HasConfiguredPasswordAsync(context.RequestAborted))
                   {
                       return RenderAdminSetupPage(context, antiforgery, safeReturnUrl);
                   }
               }
               catch
               {
                   return RenderAdminLoginPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Admin access storage is unavailable.",
                       StatusCodes.Status503ServiceUnavailable);
               }
           }

           return RenderAdminLoginPage(context, antiforgery, safeReturnUrl);
       })
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapPost(AdminAccessDefaults.LoginPath,
       async (
           HttpContext context,
           IIpBanService ipBanService,
           IAdminLoginProtectionService adminLoginProtectionService,
           IAdminCredentialService adminCredentialService,
           IAntiforgery antiforgery,
           IOptions<AdminAccessOptions> adminOptionsAccessor,
           ILogger<Program> logger,
           [FromForm]string password,
           [FromForm]string? confirmPassword,
           [FromForm]string? returnUrl) =>
       {
           AdminLoginFormInput loginForm = new(password, confirmPassword, returnUrl);

           if(!TryValidateInput(loginForm, out string validationError))
           {
               string fallbackReturnUrl = NormalizeReturnUrl(returnUrl);

               return RenderAdminLoginPage(
                   context,
                   antiforgery,
                   fallbackReturnUrl,
                   validationError,
                   StatusCodes.Status400BadRequest);
           }

           AdminAccessOptions adminOptions = adminOptionsAccessor.Value;
           string safeReturnUrl = NormalizeReturnUrl(returnUrl);
           string clientIp = RequestProtectionClassifier.ResolveClientIp(context);

           bool isBlocked;
           try
           {
               isBlocked = await ipBanService.IsBlockedAsync(clientIp, context.RequestAborted);
           }
           catch (Exception ex)
           {
               logger.LogWarning(ex, "Admin IP ban lookup failed. Continuing without ban enforcement.");
               isBlocked = false;
           }

           if(isBlocked)
           {
               return RenderAdminLoginPage(context, antiforgery, safeReturnUrl, BuildAdminLockoutMessage(), StatusCodes.Status403Forbidden);
           }

           if(context.User.Identity?.IsAuthenticated == true)
           {
               return Results.LocalRedirect(safeReturnUrl);
           }

           bool hasConfiguredPassword = configuredStaticAdminPassword is not null;

           if(configuredStaticAdminPassword is null)
           {
               try
               {
                   hasConfiguredPassword = await adminCredentialService.HasConfiguredPasswordAsync(context.RequestAborted);
               }
               catch
               {
                   return RenderAdminLoginPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Admin access storage is unavailable.",
                       StatusCodes.Status503ServiceUnavailable);
               }
           }

           if(!hasConfiguredPassword)
           {
               if(string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
               {
                   return RenderAdminSetupPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Please enter and confirm the password.",
                       StatusCodes.Status400BadRequest);
               }

               if(!string.Equals(password, confirmPassword, StringComparison.Ordinal))
               {
                   return RenderAdminSetupPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Passwords do not match.",
                       StatusCodes.Status400BadRequest);
               }

               if(!AdminPasswordPolicy.TryValidate(password, out string policyErrorMessage))
               {
                   return RenderAdminSetupPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       policyErrorMessage,
                       StatusCodes.Status400BadRequest);
               }

               try
               {
                   bool initialized = await adminCredentialService.TryInitializePasswordAsync(password, context.RequestAborted);

                   if(!initialized)
                   {
                       return Results.LocalRedirect(BuildLoginUrl(safeReturnUrl));
                   }
               }
               catch
               {
                   return RenderAdminSetupPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Admin access storage is unavailable.",
                       StatusCodes.Status503ServiceUnavailable);
               }

               await SignInAdminAsync(context, NormalizeSessionHours(adminOptions.SessionHours));
               return Results.LocalRedirect(safeReturnUrl);
           }

           bool passwordMatches;

           if(configuredStaticAdminPassword is null)
           {
               try
               {
                   passwordMatches = await adminCredentialService.VerifyPasswordAsync(password, context.RequestAborted);
               }
               catch
               {
                   return RenderAdminLoginPage(
                       context,
                       antiforgery,
                       safeReturnUrl,
                       "Admin access storage is unavailable.",
                       StatusCodes.Status503ServiceUnavailable);
               }
           }
           else
           {
               passwordMatches = PasswordMatches(password, configuredStaticAdminPassword);
           }

           if(!passwordMatches)
           {
               AdminLoginProtectionResult result;
               try
               {
                   result = await adminLoginProtectionService.RegisterFailedAttemptAsync(clientIp, context.RequestAborted);
               }
               catch (Exception ex)
               {
                   logger.LogWarning(ex, "Admin login protection update failed after invalid password.");
                   result = new AdminLoginProtectionResult(false, 0, null);
               }

               return RenderAdminLoginPage(
                   context,
                   antiforgery,
                   safeReturnUrl,
                   result.BanApplied
                   ? BuildAdminLockoutMessage(result.BanExpiresAt)
                   : "Invalid password.",
                   result.BanApplied
                   ? StatusCodes.Status403Forbidden
                   : StatusCodes.Status401Unauthorized);
           }

           try
           {
               await adminLoginProtectionService.ResetFailuresAsync(clientIp, context.RequestAborted);
           }
           catch (Exception ex)
           {
               logger.LogWarning(ex, "Admin login protection reset failed after successful login.");
           }

           await SignInAdminAsync(context, NormalizeSessionHours(adminOptions.SessionHours));
           return Results.LocalRedirect(safeReturnUrl);
       })
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapGet(AdminAccessDefaults.LegacyLoginPath,
       (string? returnUrl) => Results.LocalRedirect(BuildLoginUrl(NormalizeReturnUrl(returnUrl))))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapGet(AdminAccessDefaults.SetupPath,
       (string? returnUrl) => Results.LocalRedirect(BuildLoginUrl(NormalizeReturnUrl(returnUrl))))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapPost(AdminAccessDefaults.LegacyLoginPath,
       ([FromForm]string? returnUrl) => Results.LocalRedirect(BuildLoginUrl(NormalizeReturnUrl(returnUrl))))
   .AllowAnonymous()
   .DisableAntiforgery()
   .ExcludeFromDescription();

app.MapPost(AdminAccessDefaults.SetupPath,
       ([FromForm]string? returnUrl) => Results.LocalRedirect(BuildLoginUrl(NormalizeReturnUrl(returnUrl))))
   .AllowAnonymous()
   .DisableAntiforgery()
   .ExcludeFromDescription();

app.MapGet(AdminAccessDefaults.LogoutPath,
       async (HttpContext context) =>
       {
           await context.SignOutAsync(AdminAccessDefaults.CookieScheme);
           return Results.LocalRedirect("/");
       })
   .RequireAuthorization(AdminAccessDefaults.PolicyName)
   .ExcludeFromDescription();




app.MapPost("/api/scraper/quick-test",
       async (
           [FromServices]IScraperRuntimeService scraperRuntimeService,
           CancellationToken cancellationToken) =>
       {
           ScraperStartResult result = await scraperRuntimeService.StartAsync(
               new ScraperRunRequest(TriggeredBy: "QuickTest"),
               cancellationToken);

           return result.Started
           ? Results.Accepted(value: new
           {
               message = result.Message,
               status = result.Status
           })
           : Results.Conflict(new
           {
               message = result.Message,
               status = result.Status
           });
       })
   .RequireAuthorization(AdminAccessDefaults.PolicyName)
   .WithGroupName(AdminAccessDefaults.AdminDocumentName);

await app.RunAsync();

static string NormalizeReturnUrl(string? returnUrl)
{
    if(string.IsNullOrWhiteSpace(returnUrl))
    {
        return AdminAccessDefaults.AdminDashboardPath;
    }

    string trimmedReturnUrl = returnUrl.Trim();

    if(!trimmedReturnUrl.StartsWith("/", StringComparison.Ordinal) ||
       trimmedReturnUrl.StartsWith("//", StringComparison.Ordinal) ||
       trimmedReturnUrl.Contains('\\') ||
       trimmedReturnUrl.Contains('\r') ||
       trimmedReturnUrl.Contains('\n'))
    {
        return AdminAccessDefaults.AdminDashboardPath;
    }

    return IsAllowedAdminReturnPath(trimmedReturnUrl)
    ? trimmedReturnUrl
    : AdminAccessDefaults.AdminDashboardPath;
}

static string BuildLoginUrl(string returnUrl)
{
    string safeReturnUrl = NormalizeReturnUrl(returnUrl);
    return $"{AdminAccessDefaults.LoginPath}?returnUrl={Uri.EscapeDataString(safeReturnUrl)}";
}

static string? ResolveStaticAdminPassword(IHostEnvironment environment)
{
    if(environment.IsDevelopment())
    {
        return AdminAccessDefaults.DevelopmentPassword;
    }

    return null;
}

static bool PasswordMatches(string providedPassword, string configuredPassword)
{
    byte[] providedBytes = Encoding.UTF8.GetBytes(providedPassword ?? string.Empty);
    byte[] configuredBytes = Encoding.UTF8.GetBytes(configuredPassword ?? string.Empty);
    int maxLen = Math.Max(providedBytes.Length, configuredBytes.Length);
    byte[] paddedProvided = new byte[maxLen];
    byte[] paddedConfigured = new byte[maxLen];
    providedBytes.CopyTo(paddedProvided, 0);
    configuredBytes.CopyTo(paddedConfigured, 0);
    return CryptographicOperations.FixedTimeEquals(paddedProvided, paddedConfigured);
}

static async Task SignInAdminAsync(HttpContext context, int sessionHours)
{
    ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(AdminAccessDefaults.ClaimType, AdminAccessDefaults.ClaimValue),
            new Claim(ClaimTypes.Name, "Admin")
        ],
        AdminAccessDefaults.CookieScheme));

    AuthenticationProperties properties = new()
    {
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(NormalizeSessionHours(sessionHours)),
        IsPersistent = false,
        AllowRefresh = false
    };

    await context.SignInAsync(AdminAccessDefaults.CookieScheme, principal, properties);
}

static IResult RenderAdminLoginPage(
    HttpContext context,
    IAntiforgery antiforgery,
    string returnUrl,
    string? errorMessage = null,
    int statusCode = StatusCodes.Status200OK)
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);

    return Results.Content(
        AdminLoginPageRenderer.Render(returnUrl, tokens.RequestToken ?? string.Empty, errorMessage),
        "text/html; charset=utf-8",
        Encoding.UTF8,
        statusCode);
}

static IResult RenderAdminSetupPage(
    HttpContext context,
    IAntiforgery antiforgery,
    string returnUrl,
    string? errorMessage = null,
    int statusCode = StatusCodes.Status200OK)
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);

    return Results.Content(
        AdminSetupPageRenderer.Render(returnUrl, tokens.RequestToken ?? string.Empty, errorMessage),
        "text/html; charset=utf-8",
        Encoding.UTF8,
        statusCode);
}

static bool TryValidateInput<TInput>(TInput input, out string validationError)
{
    ValidationContext validationContext = new(input!);
    List<ValidationResult> validationResults = [];

    if(Validator.TryValidateObject(input!, validationContext, validationResults, true))
    {
        validationError = string.Empty;
        return true;
    }

    validationError = validationResults.FirstOrDefault()?.ErrorMessage ?? "The submitted input is invalid.";
    return false;
}

static int NormalizeSessionHours(int sessionHours)
{
    return Math.Clamp(sessionHours, 1, 24);
}

static bool IsAllowedAdminReturnPath(string returnUrl)
{
    return returnUrl.Equals(AdminAccessDefaults.AdminDashboardPath, StringComparison.OrdinalIgnoreCase) ||
           returnUrl.StartsWith($"{AdminAccessDefaults.AdminDashboardPath}/", StringComparison.OrdinalIgnoreCase) ||
           returnUrl.Equals(AdminAccessDefaults.AdminScalarPath, StringComparison.OrdinalIgnoreCase) ||
           returnUrl.Equals(AdminAccessDefaults.AdminOpenApiPath, StringComparison.OrdinalIgnoreCase);
}

static string BuildPublicScalarHeaderContent()
{
    string loginUrl = $"{AdminAccessDefaults.LoginPath}?returnUrl={Uri.EscapeDataString(AdminAccessDefaults.AdminDashboardPath)}";

    return $$"""
             <div style="position:fixed;top:16px;right:20px;z-index:2147483647;pointer-events:none;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
                 <a href="{{loginUrl}}" style="pointer-events:auto;display:inline-flex;align-items:center;justify-content:center;padding:10px 16px;border-radius:999px;background:linear-gradient(135deg,#2563eb 0%,#1d4ed8 100%);color:#ffffff;text-decoration:none;font-size:14px;font-weight:700;letter-spacing:.01em;border:1px solid rgba(255,255,255,.22);box-shadow:0 14px 32px rgba(15,23,42,.35),0 2px 8px rgba(37,99,235,.35);backdrop-filter:blur(10px);-webkit-backdrop-filter:blur(10px);white-space:nowrap;">Admin</a>
             </div>
             """;
}

static string BuildAdminLockoutMessage(DateTime? banExpiresAt = null)
{
    return banExpiresAt is null
    ? "Too many failed password attempts. Admin access is temporarily blocked."
    : $"Too many failed password attempts. Admin access is blocked until {banExpiresAt.Value:yyyy-MM-dd HH:mm:ss} UTC.";
}

static string BuildMinuteCronExpression(int intervalMinutes)
{
    int normalizedInterval = Math.Max(1, intervalMinutes);
    return normalizedInterval == 1
    ? "* * * * *"
    : $"*/{normalizedInterval} * * * *";
}

static void TryRegisterDotNetMetrics()
{
    MethodInfo? registerDefaultMethod = typeof(DotNetStats).GetMethod(
        "RegisterDefault",
        BindingFlags.Public | BindingFlags.Static);

    registerDefaultMethod?.Invoke(null, null);
}

static bool TryParseIpNetwork(string value, out System.Net.IPNetwork? network)
{
    network = null;

    if(string.IsNullOrWhiteSpace(value))
    {
        return false;
    }

    string[] parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if(parts.Length != 2 ||
       !IPAddress.TryParse(parts[0], out IPAddress? prefix) ||
       !int.TryParse(parts[1], out int prefixLength))
    {
        return false;
    }

    network = new System.Net.IPNetwork(prefix, prefixLength);
    return true;
}

namespace TibiaDataApi
{
    public class Program;

    internal sealed class AdminLoginFormInput
    {
        public AdminLoginFormInput(string password, string? confirmPassword, string? returnUrl)
        {
            Password = password;
            ConfirmPassword = confirmPassword;
            ReturnUrl = returnUrl;
        }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(256, MinimumLength = 1, ErrorMessage = "Password length is invalid.")]
        public string Password { get; }

        [StringLength(256, ErrorMessage = "Confirm password length is invalid.")]
        public string? ConfirmPassword { get; }

        [StringLength(512, ErrorMessage = "Return URL is too long.")]
        public string? ReturnUrl { get; }
    }
}