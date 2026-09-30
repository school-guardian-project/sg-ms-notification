using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Configuration;

public class AlertRecipientConfiguration : IEntityTypeConfiguration<AlertRecipient>
{
    public void Configure(EntityTypeBuilder<AlertRecipient> builder)
    {
        builder.ToTable("AlertRecipient");
        builder.HasKey(e => e.Id);
        builder.HasOne<Alert>().WithMany().HasForeignKey(e => e.AlertId);
    }
}
