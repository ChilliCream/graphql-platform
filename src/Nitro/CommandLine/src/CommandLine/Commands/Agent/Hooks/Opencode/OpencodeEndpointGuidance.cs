namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

/// <summary>
/// Guidance for enabling the Opencode HTTP endpoint used for push delivery.
/// </summary>
internal static class OpencodeEndpointGuidance
{
    private const string RemedyFlags = "--port, --hostname, or --mdns";

    /// <summary>
    /// Endpoint setup guidance included in the install result or human-readable output.
    /// </summary>
    public const string InstallNote =
        "Nitro can only push to opencode when it binds an HTTP server: start it "
        + "with an explicit " + RemedyFlags + " flag.";

    /// <summary>
    /// Guidance printed by status for sessions with no registered endpoint,
    /// regardless of their last recorded ping.
    /// </summary>
    public const string SessionRemedy =
        "Pushes will not arrive for this session: start opencode with an explicit "
        + RemedyFlags + " flag.";
}
