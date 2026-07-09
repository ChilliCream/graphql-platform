namespace ChilliCream.Nitro.CommandLine.Services;

internal interface IEnvironmentVariableProvider
{
    string? GetEnvironmentVariable(string name);
}
