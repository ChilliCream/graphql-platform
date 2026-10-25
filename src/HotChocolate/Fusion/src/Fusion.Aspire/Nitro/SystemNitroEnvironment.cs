namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// An <see cref="INitroEnvironment"/> that reads the environment variables of the
/// current process.
/// </summary>
internal sealed class SystemNitroEnvironment : INitroEnvironment
{
    public static SystemNitroEnvironment Instance { get; } = new();

    public string? GetVariable(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return Environment.GetEnvironmentVariable(name);
    }
}
