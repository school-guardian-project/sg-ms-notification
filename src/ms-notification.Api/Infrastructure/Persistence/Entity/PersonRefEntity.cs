namespace ms_notification.Api.Infrastructure.Persistence.Entity;

public class PersonRefEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
