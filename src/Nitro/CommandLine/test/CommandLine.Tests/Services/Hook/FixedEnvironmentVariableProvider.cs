using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// An in-memory <see cref="IEnvironmentVariableProvider"/> for tests that
/// need to control specific variables (<c>NITRO_HOOK_SUPPRESS</c>,
/// <c>NITRO_HOOK_SUPPRESS</c>) without touching the real process environment.
/// </summary>
internal sealed class FixedEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private readonly Dictionary<string, string> _values = [];

    public void Set(string name, string value) => _values[name] = value;

    public string? GetEnvironmentVariable(string name) => _values.GetValueOrDefault(name);
}
