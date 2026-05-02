using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

using TibiaDataApi.OutputCaching;
using TibiaDataApi.RequestProtection;

namespace TibiaDataApi.Controller.Public
{
    [EnableRateLimiting(RequestProtectionRateLimiter.PublicApiPolicyName)]
    [OutputCache(PolicyName = OutputCacheDefaults.PublicApiPolicyName)]
    public abstract class PublicApiControllerBase : ControllerBase
    {
    }
}
