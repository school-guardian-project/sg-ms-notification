using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Configuration;

public class BoardingConfiguration : IEntityTypeConfiguration<Boarding>
{
    public void Configure(EntityTypeBuilder<Boarding> builder)
    {
        builder.ToTable("Boarding");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BoardingType).HasMaxLength(20);
    }
}
