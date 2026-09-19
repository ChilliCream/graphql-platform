namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// The recipient's local Nitro instance wake generation committed with a send or reply.
/// It records wake intent without guaranteeing delivery of the message in a digest.
/// </summary>
internal sealed record MailWakeReceipt
{
    public required string Actor { get; init; }
    public required long Generation { get; init; }
}
