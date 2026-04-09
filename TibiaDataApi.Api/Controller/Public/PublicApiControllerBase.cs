using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using TibiaDataApi.RequestProtection;

namespace TibiaDataApi.Controller.Public
{
        [EnableRateLimiting(RequestProtectionRateLimiter.PublicApiPolicyName)]
    public abstract class PublicApiControllerBase : ControllerBase
    {
    }
}
