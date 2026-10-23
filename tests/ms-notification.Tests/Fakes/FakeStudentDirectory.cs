using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Port.Out;

namespace ms_notification.Tests.Fakes;

internal sealed class FakeStudentDirectory : IStudentDirectory
{
    public string? StudentName { get; set; } = "Juan Pérez";

    public Task<string?> GetStudentNameAsync(Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult(StudentName);

    public Task<StudentInfo?> GetStudentByProfileIdAsync(Guid studentProfileId, CancellationToken ct = default)
        => Task.FromResult(StudentName != null
            ? new StudentInfo(Guid.NewGuid(), StudentName, null, null)
            : null);
}
