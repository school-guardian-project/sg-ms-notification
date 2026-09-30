using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Configuration;

public class AlertTypeConfiguration : IEntityTypeConfiguration<AlertType>
{
    public void Configure(EntityTypeBuilder<AlertType> builder)
    {
        builder.ToTable("AlertType");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(100).IsRequired();
    }
}
