namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// Maps a bounded machine wake reason to a bounded human-readable detail.
/// Every value is a stable, safe description; never a raw exception message
/// or subprocess stderr.
/// </summary>
internal static class MailWakeReasonText
{
    public static string Describe(string reason) => reason switch
    {
        "session-gone" => "The session ended before the wake could be attempted.",
        "no-endpoint" => "The session has no endpoint the wake could reach.",
        "unsupported" or "Unsupported" => "The session's endpoint does not support the automatic wake.",
        "busy" => "The session's transport was already busy with another attempt.",
        "capacity-dropped" or "CapacityDropped" => "No wake transport capacity was available.",
        "access-denied" or "AccessDenied" => "Access to the local Claude endpoint was denied.",
        "mail-already-read" => "The recipient had already read the mail before the wake ran.",
        "EndpointGone" => "The session's endpoint disappeared before the wake could be attempted.",
        "InvalidAuth" => "The session's endpoint rejected the wake's authentication.",
        "Timeout" => "The wake attempt did not complete before its deadline.",
        "TransportError" => "The wake attempt failed to reach the session's endpoint.",
        _ => $"The wake attempt did not complete ({reason})."
    };
}
