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

    public IQueryable<Alert> GetSchoolAlerts(Guid? adminProfileId, Guid? schoolId)
    {
        var query = Alerts.AsNoTracking();
        if (!adminProfileId.HasValue && !schoolId.HasValue)
            return query;

        var schools = Set<SchoolAdminRefEntity>()
            .Where(a => a.ProfileId == adminProfileId && a.Status == "Active")
            .Select(a => a.SchoolId);
        var profiles =
            from profile in Set<ProfileRefEntity>()
            join campus in Set<SchoolCampusRefEntity>()
                on profile.CampuseId equals campus.Id
            where (!adminProfileId.HasValue || schools.Contains(campus.SchoolId))
                && (!schoolId.HasValue || campus.SchoolId == schoolId)
            select profile.Id;
        return query.Where(a => profiles.Contains(a.ProfileId));
    }

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

        // Solo lectura, para resolver el colegio del conductor que reporta (FR-8.2).
        modelBuilder.Entity<SchoolCampusRefEntity>()
            .HasNoKey()
            .ToTable("SchoolCampus", schema: "School");

        modelBuilder.Entity<SchoolAdminRefEntity>()
            .HasNoKey()
            .ToTable("SchoolAdmin", schema: "School");
    }
}
