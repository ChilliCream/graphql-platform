namespace ChilliCream.Nitro.CommandLine.Services.Mail;

internal sealed record MailTransferResult(
    int RecipientsMoved,
    int SendersMoved,
    int Dropped);
