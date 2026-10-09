namespace ChilliCream.Nitro.CommandLine.Services.Mail;

internal sealed record MailTransferResult(
    int RecipientsMoved,
    int Dropped)
{
    public IReadOnlyList<string> RecipientMessageIds { get; init; } = [];
}
