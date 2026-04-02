namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable(string name)
        => Environment.GetEnvironmentVariable(name);
}
