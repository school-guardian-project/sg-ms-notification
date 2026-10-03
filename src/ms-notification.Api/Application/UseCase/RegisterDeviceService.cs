using Microsoft.EntityFrameworkCore;
using ms_notification.Api.Application.Dto;
using ms_notification.Api.Domain.Model;
using ms_notification.Api.Infrastructure.Persistence.Context;

namespace ms_notification.Api.Application.UseCase;

public class RegisterDeviceService
{
    private readonly NotificationContext _context;

    public RegisterDeviceService(NotificationContext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(RegisterDeviceRequestDto request, CancellationToken ct = default)
    {
        if (request.ProfileId == Guid.Empty)
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
            device = new DeviceToken
            {
                Id = Guid.NewGuid(),
                ExpoToken = request.ExpoToken
            };

            _context.DeviceTokens.Add(device);
        }

        device.ProfileId = request.ProfileId;
        device.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}
