namespace ChilliCream.Nitro.CommandLine.Services.Mail;

internal sealed record MailTransferResult(
    int RecipientsMoved,
    int SendersMoved,
    int Dropped)
{
    public IReadOnlyList<string> SenderMessageIds { get; init; } = [];

    public IReadOnlyList<string> RecipientMessageIds { get; init; } = [];
}
