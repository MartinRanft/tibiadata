using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Security;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class AdminLoginFailureConfiguration : IEntityTypeConfiguration<AdminLoginFailure>
    {
        public void Configure(EntityTypeBuilder<AdminLoginFailure> builder)
        {
            builder.ToTable("admin_login_failures");

            builder.HasKey(entry => entry.IpAddress);

            builder.Property(entry => entry.IpAddress)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.FailedAttempts)
                   .IsRequired();

            builder.Property(entry => entry.FirstFailedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.LastFailedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();
        }
    }
}
