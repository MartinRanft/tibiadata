using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Security;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class IpBanConfiguration : IEntityTypeConfiguration<IpBan>
    {
        public void Configure(EntityTypeBuilder<IpBan> builder)
        {
            builder.ToTable("ip_bans");

            builder.HasKey(entry => entry.Id);

            builder.Property(entry => entry.IpAddress)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.Reason)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(entry => entry.CreatedBy)
                   .HasMaxLength(100);

            builder.Property(entry => entry.RevokedBy)
                   .HasMaxLength(100);

            builder.Property(entry => entry.RevocationReason)
                   .HasMaxLength(500);

            builder.Property(entry => entry.CreatedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.ExpiresAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.DurationMinutes);

            builder.Property(entry => entry.RevokedAt)
                   .HasProviderDateTimeColumnType();

            builder.HasIndex(entry => entry.IpAddress);
            builder.HasIndex(entry => new
            {
                entry.IpAddress,
                entry.IsActive
            });
        }
    }
}
