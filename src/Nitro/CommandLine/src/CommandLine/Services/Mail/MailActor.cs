namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Resolves the acting agent address recorded on mail mutations.
/// </summary>
internal static class MailActor
{
    public const string EnvironmentVariableName = "NITRO_MAIL_ACTOR";
    public const string FallbackEnvironmentVariableName = "NITRO_TASK_ACTOR";

    /// <summary>
    /// Resolves the actor from the given option value, the
    /// NITRO_MAIL_ACTOR environment variable, the NITRO_TASK_ACTOR
    /// environment variable, or the OS user name, in that order, then
    /// normalizes it with <see cref="MailAgentName.Normalize"/>. The
    /// normalized result is the agent's address.
    /// </summary>
    public static string Resolve(
        string? optionValue,
        IEnvironmentVariableProvider environmentVariables)
    {
        var resolved = optionValue
            ?? environmentVariables.GetEnvironmentVariable(EnvironmentVariableName)
            ?? environmentVariables.GetEnvironmentVariable(FallbackEnvironmentVariableName)
            ?? Environment.UserName;

        return MailAgentName.Normalize(resolved);
    }

    /// <summary>
    /// Resolves the actor from the given option value, the
    /// NITRO_MAIL_ACTOR environment variable, or the NITRO_TASK_ACTOR
    /// environment variable, in that order, then normalizes it with
    /// <see cref="MailAgentName.Normalize"/>. Returns null when none of them
    /// are set: unlike <see cref="Resolve"/>, this does not fall back to the
    /// OS user name.
    /// </summary>
    public static string? TryResolve(
        string? optionValue,
        IEnvironmentVariableProvider environmentVariables)
    {
        var resolved = optionValue
            ?? environmentVariables.GetEnvironmentVariable(EnvironmentVariableName)
            ?? environmentVariables.GetEnvironmentVariable(FallbackEnvironmentVariableName);

        return resolved is null ? null : MailAgentName.Normalize(resolved);
    }
}
