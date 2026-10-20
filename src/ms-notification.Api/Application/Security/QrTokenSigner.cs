using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ms_notification.Api.Application.Security;

public sealed class QrTokenSigner
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly byte[] _secret;
    private readonly TimeSpan _ttl;

    public QrTokenSigner(IConfiguration configuration)
    {
        var secret = configuration["Qr:Secret"]
            ?? throw new InvalidOperationException("Missing Qr__Secret (set it in .env)");

        var ttlRaw = configuration["Qr:TtlSeconds"]
            ?? throw new InvalidOperationException("Missing Qr__TtlSeconds (set it in .env)");

        if (!int.TryParse(ttlRaw, out var ttlSeconds))
        {
            throw new InvalidOperationException("Invalid Qr__TtlSeconds: must be a whole number of seconds");
        }

        _secret = Encoding.UTF8.GetBytes(secret);
        _ttl = TimeSpan.FromSeconds(ttlSeconds);
    }

    public IssuedQrToken Issue(Guid studentProfileId, string studentName, Guid routeId, Guid routeStopId)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new QrPayload
        {
            StudentProfileId = studentProfileId.ToString(),
            StudentName = studentName,
            RouteId = routeId.ToString(),
            RouteStopId = routeStopId.ToString(),
            IssuedAt = now.ToUnixTimeSeconds(),
            ExpiresAt = now.Add(_ttl).ToUnixTimeSeconds()
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var body = Base64Url(json);
        var signature = Base64Url(Hmac(Encoding.UTF8.GetBytes(body)));

        return new IssuedQrToken($"{body}.{signature}", now.Add(_ttl));
    }

    public QrPayload? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        byte[] provided;
        try
        {
            provided = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            return null;
        }

        var expected = Hmac(Encoding.UTF8.GetBytes(parts[0]));
        if (!CryptographicOperations.FixedTimeEquals(expected, provided))
        {
            return null;
        }

        QrPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<QrPayload>(FromBase64Url(parts[0]), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload is null || payload.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return null;
        }

        return payload;
    }

    private byte[] Hmac(byte[] data)
    {
        using var hmac = new HMACSHA256(_secret);
        return hmac.ComputeHash(data);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}