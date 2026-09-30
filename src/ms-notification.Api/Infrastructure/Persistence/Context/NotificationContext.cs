using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Model;

namespace ms_notification.Api.Infrastructure.Persistence.Context;

public class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options) { }

    public DbSet<Boarding> Boardings { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AlertRecipient> AlertRecipients { get; set; }
    public DbSet<AlertType> AlertTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Notification");

        modelBuilder.Entity<AlertType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(100);
            entity.HasOne<AlertType>().WithMany().HasForeignKey(e => e.AlertTypeId);
        });

        modelBuilder.Entity<AlertRecipient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<Alert>().WithMany().HasForeignKey(e => e.AlertId);
        });

        modelBuilder.Entity<Boarding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BoardingType).HasMaxLength(20);
        });
    }
}
