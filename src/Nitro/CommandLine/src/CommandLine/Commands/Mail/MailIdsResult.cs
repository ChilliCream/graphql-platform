namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

/// <summary>
/// The message IDs a batch mutation (ack, archive) acted on, as returned by
/// its structured (JSON) output.
/// </summary>
internal sealed record MailIdsResult(IReadOnlyList<string> Ids);
