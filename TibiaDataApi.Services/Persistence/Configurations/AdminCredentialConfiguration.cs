using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaDataApi.Services.Entities.Security;

namespace TibiaDataApi.Services.Persistence.Configurations
{
    public sealed class AdminCredentialConfiguration : IEntityTypeConfiguration<AdminCredential>
    {
        public void Configure(EntityTypeBuilder<AdminCredential> builder)
        {
            builder.ToTable("admin_credentials");

            builder.HasKey(entry => entry.Key);

            builder.Property(entry => entry.Key)
                   .IsRequired()
                   .HasMaxLength(64);

            builder.Property(entry => entry.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.Property(entry => entry.CreatedAt)
                   .HasProviderDateTimeColumnType();

            builder.Property(entry => entry.UpdatedAt)
                   .HasProviderDateTimeColumnType();
        }
    }
}
