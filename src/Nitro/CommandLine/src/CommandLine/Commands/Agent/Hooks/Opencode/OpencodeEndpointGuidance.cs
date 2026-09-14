namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;

/// <summary>
/// The one remedy Nitro has for an opencode session it cannot push to: a
/// plain <c>opencode</c> TUI reaches its own server inside a Worker over
/// postMessage RPC and binds no HTTP server of its own unless started with
/// one of these flags (see
/// <c>EndpointAddress.IsTrustedOpencodeServerUrl</c>), so this is the only
/// actionable fix a user has. Shared verbatim between
/// <see cref="InstallOpencodeHooksCommand"/> (an upfront, unconditional
/// note) and <see cref="StatusOpencodeHooksCommand"/> (a per-session
/// warning) so the wording never drifts between the two moments a user
/// would look.
/// </summary>
internal static class OpencodeEndpointGuidance
{
    private const string RemedyFlags = "--port, --hostname, or --mdns";

    /// <summary>
    /// Printed once, unconditionally, by <c>install</c>: opencode's HTTP
    /// server binding is a prerequisite for every future push, stated
    /// upfront before any session has even started.
    /// </summary>
    public const string InstallNote =
        "Nitro can only push to opencode when it binds an HTTP server: start it "
        + "with an explicit " + RemedyFlags + " flag.";

    /// <summary>
    /// Printed by <c>status</c> next to a session whose endpoint is the
    /// unproven placeholder (<c>endpoint_kind = 'none'</c>) - the only case
    /// Nitro can say for certain that a push has nowhere to go. A session
    /// with a registered endpoint never gets this line, no matter what its
    /// last recorded ping says: a failed ping is one recorded attempt, not
    /// proof the endpoint cannot be reached (see
    /// <c>StatusOpencodeHooksCommand.OpencodeSessionReachability</c>).
    /// </summary>
    public const string SessionRemedy =
        "Pushes will not arrive for this session: start opencode with an explicit "
        + RemedyFlags + " flag.";
}
