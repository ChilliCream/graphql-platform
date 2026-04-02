namespace ChilliCream.Nitro.CommandLine.Helpers;

internal interface IEnvironmentVariableProvider
{
    string? GetEnvironmentVariable(string name);
}
