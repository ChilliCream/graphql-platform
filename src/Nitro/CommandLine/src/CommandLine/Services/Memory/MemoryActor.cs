namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Resolves the acting identity recorded on memory writes.
/// </summary>
internal static class MemoryActor
{
    public const string EnvironmentVariableName = "NITRO_MEMORY_ACTOR";
    public const string FallbackEnvironmentVariableName = "NITRO_TASK_ACTOR";

    /// <summary>
    /// Resolves the actor from the given option value, the
    /// NITRO_MEMORY_ACTOR environment variable, the NITRO_TASK_ACTOR
    /// environment variable, or the OS user name, in that order.
    /// </summary>
    public static string Resolve(
        string? optionValue,
        IEnvironmentVariableProvider environmentVariables)
        => optionValue
            ?? environmentVariables.GetEnvironmentVariable(EnvironmentVariableName)
            ?? environmentVariables.GetEnvironmentVariable(FallbackEnvironmentVariableName)
            ?? Environment.UserName;
}
