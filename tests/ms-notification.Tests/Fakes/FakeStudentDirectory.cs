using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

internal sealed class FakeStudentDirectory : IStudentDirectory
{
    public string? StudentName { get; set; } = "Juan Pérez";

    public Task<string?> GetStudentNameAsync(Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult(StudentName);
}
