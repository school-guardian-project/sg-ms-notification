using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ms_notification.Tests.Fakes;

/// <summary>
/// Minimal configuration for the tests: QrTokenSigner only reads two keys through the
/// indexer, so the rest of the IConfiguration contract is deliberately unsupported.
/// </summary>
internal sealed class FakeConfiguration : IConfiguration
{
    private readonly Dictionary<string, string?> _values;

    public FakeConfiguration(Dictionary<string, string?> values) => _values = values;

    public string? this[string key]
    {
        get => _values.TryGetValue(key, out var value) ? value : null;
        set => _values[key] = value;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken() => throw new NotSupportedException();

    public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
}
