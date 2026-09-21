using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// An in-memory <see cref="IEnvironmentVariableProvider"/> that returns null
/// for unset variables and leaves the process environment unchanged.
/// </summary>
internal sealed class FixedEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private readonly Dictionary<string, string> _values = [];

    public void Set(string name, string value) => _values[name] = value;

    public string? GetEnvironmentVariable(string name) => _values.GetValueOrDefault(name);
}
