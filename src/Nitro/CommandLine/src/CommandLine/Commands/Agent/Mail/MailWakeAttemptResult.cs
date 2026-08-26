namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// A bounded, stable machine <see cref="Reason"/> and a bounded human
/// <see cref="Detail"/> for one wake target or recipient outcome. Never a raw
/// exception message or subprocess stderr.
/// </summary>
internal sealed record MailWakeAttemptResult
{
    public required string Reason { get; init; }
    public required string Detail { get; init; }

    public static MailWakeAttemptResult? Create(string? reason) =>
        reason is null ? null : new MailWakeAttemptResult { Reason = reason, Detail = MailWakeReasonText.Describe(reason) };
}
