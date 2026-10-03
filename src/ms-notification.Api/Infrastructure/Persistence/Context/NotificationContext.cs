using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Configuration;
using ms_notification.Api.Infrastructure.Persistence.Entity;

namespace ms_notification.Api.Infrastructure.Persistence.Context;

public class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options) { }

    public DbSet<Boarding> Boardings { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AlertRecipient> AlertRecipients { get; set; }
    public DbSet<AlertType> AlertTypes { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Notification");

        modelBuilder.ApplyConfiguration(new AlertTypeConfiguration());
        modelBuilder.ApplyConfiguration(new AlertConfiguration());
        modelBuilder.ApplyConfiguration(new AlertRecipientConfiguration());
        modelBuilder.ApplyConfiguration(new BoardingConfiguration());
        modelBuilder.ApplyConfiguration(new DeviceTokenConfiguration());

        modelBuilder.Entity<ProfileRefEntity>()
            .HasNoKey()
            .ToTable("Profile", schema: "Iam");

        modelBuilder.Entity<PersonRefEntity>()
            .HasNoKey()
            .ToTable("Person", schema: "UserManagement");
    }
}
