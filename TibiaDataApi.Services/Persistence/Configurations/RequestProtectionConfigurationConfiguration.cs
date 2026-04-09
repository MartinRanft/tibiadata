using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Security;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class RequestProtectionConfigurationConfiguration : IEntityTypeConfiguration<RequestProtectionConfiguration>
    {
        public void Configure(EntityTypeBuilder<RequestProtectionConfiguration> builder)
        {
            builder.ToTable("request_protection_configurations");

            builder.HasKey(entry => entry.Key);

            builder.Property(entry => entry.Key)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.CreatedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();
        }
    }
}
