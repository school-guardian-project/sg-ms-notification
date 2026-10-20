using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Domain.Port.In;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class RegisterDeviceService : IRegisterDeviceUseCase
{
    private readonly NotificationContext _context;

    public RegisterDeviceService(NotificationContext context)
    {
        _context = context;
    }

    // El profileId viene del JWT, no del body: no confiamos en el cliente.
    public async Task ExecuteAsync(Guid profileId, RegisterDeviceRequestDto request, CancellationToken ct = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new InvalidOperationException("ProfileId is required");
        }

        if (string.IsNullOrWhiteSpace(request.ExpoToken))
        {
            throw new InvalidOperationException("ExpoToken is required");
        }

        var device = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.ExpoToken == request.ExpoToken, ct);

        if (device is null)
        {
            device = new DeviceToken { Id = Guid.NewGuid(), ExpoToken = request.ExpoToken };
            _context.DeviceTokens.Add(device);
        }

        device.ProfileId = profileId;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}
