namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

/// <summary>
/// The one remedy Nitro has for an opencode session it cannot push to. Shared verbatim
/// between <see cref="InstallOpencodeHooksCommand"/> and <see cref="StatusOpencodeHooksCommand"/>.
/// </summary>
internal static class OpencodeEndpointGuidance
{
    private const string RemedyFlags = "--port, --hostname, or --mdns";

    /// <summary>
    /// Printed once, unconditionally, by <c>install</c>: opencode's HTTP server binding
    /// is a prerequisite for every future push.
    /// </summary>
    public const string InstallNote =
        "Nitro can only push to opencode when it binds an HTTP server: start it "
        + "with an explicit " + RemedyFlags + " flag.";

    /// <summary>
    /// Printed by <c>status</c> next to a session whose endpoint is the unproven
    /// placeholder (<c>endpoint_kind = 'none'</c>). A session with a registered endpoint
    /// never gets this line, regardless of its last recorded ping.
    /// </summary>
    public const string SessionRemedy =
        "Pushes will not arrive for this session: start opencode with an explicit "
        + RemedyFlags + " flag.";
}
