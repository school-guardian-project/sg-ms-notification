namespace ms_notification.Api.Infrastructure.Persistence.Entity;

public class ProfileRefEntity
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }

    /// <summary>Campus al que pertenece el perfil; lleva al colegio (FR-8.2).</summary>
    public Guid CampuseId { get; set; }
}
