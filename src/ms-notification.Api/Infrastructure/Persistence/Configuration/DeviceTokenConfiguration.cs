using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Configuration;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("DeviceToken");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ProfileId).IsRequired();
        builder.Property(e => e.ExpoToken).HasMaxLength(300).IsRequired();
        builder.HasIndex(e => e.ExpoToken).IsUnique();
        builder.Property(e => e.UpdatedAt).IsRequired();
    }
}
