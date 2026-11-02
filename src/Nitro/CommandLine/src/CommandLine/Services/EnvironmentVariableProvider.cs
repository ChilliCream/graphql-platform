namespace ChilliCream.Nitro.CommandLine.Services;

internal sealed class EnvironmentVariableProvider : IEnvironmentVariableProvider
{
    public string? GetEnvironmentVariable(string name)
        => Environment.GetEnvironmentVariable(name);
}
