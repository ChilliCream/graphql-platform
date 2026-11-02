namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// An <see cref="INitroEnvironment"/> that serves values from a dictionary so tests never mutate
/// the environment of the test process.
/// </summary>
internal sealed class TestNitroEnvironment : INitroEnvironment
{
    private readonly Dictionary<string, string?> _variables;

    public TestNitroEnvironment(params (string Name, string? Value)[] variables)
    {
        _variables = variables.ToDictionary(
            variable => variable.Name,
            variable => variable.Value,
            StringComparer.Ordinal);
    }

    public string? GetVariable(string name)
        => _variables.TryGetValue(name, out var value) ? value : null;
}
