using System.ComponentModel.DataAnnotations;

namespace TibiaDataApi.AdminAccess
{
    public sealed class AdminAccessOptions
    {
        public const string SectionName = "AdminAccess";

        [Required] [MaxLength(128)]public string CookieName { get; set; } = AdminAccessDefaults.DefaultCookieName;

        [Range(1, 24)]public int SessionHours { get; set; } = 24;
    }
}