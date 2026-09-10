namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The environment variables that the Nitro CLI and the Nitro integration share. The names
/// match the Nitro CLI (prefix plus option name), so a shell that is set up for the CLI also
/// configures the Aspire integration.
/// </summary>
internal static class NitroEnvironmentVariables
{
    /// <summary>
    /// Overrides the Nitro API URL.
    /// </summary>
    public const string CloudUrl = "NITRO_CLOUD_URL";

    /// <summary>
    /// Provides an API key instead of an interactive session.
    /// </summary>
    public const string ApiKey = "NITRO_API_KEY";
}
