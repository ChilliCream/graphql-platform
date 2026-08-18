namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Resolves the acting identity recorded on task mutations.
/// </summary>
internal static class TaskActor
{
    public const string EnvironmentVariableName = "TASK_ACTOR";

    /// <summary>
    /// Resolves the actor from the given option value, the NITRO_TASK_ACTOR
    /// environment variable, or the OS user name, in that order.
    /// </summary>
    public static string Resolve(
        string? optionValue,
        IEnvironmentVariableProvider environmentVariables)
        => optionValue
            ?? environmentVariables.GetEnvironmentVariable(
                "NITRO_" + EnvironmentVariableName)
            ?? Environment.UserName;
}
