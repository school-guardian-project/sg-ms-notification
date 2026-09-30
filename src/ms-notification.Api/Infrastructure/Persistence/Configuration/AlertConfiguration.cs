using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Configuration;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alert");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(100);
        builder.HasOne<AlertType>().WithMany().HasForeignKey(e => e.AlertTypeId);
    }
}
