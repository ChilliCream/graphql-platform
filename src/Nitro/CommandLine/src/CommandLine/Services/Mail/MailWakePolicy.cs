namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Whether a send or reply advances recipients' wake generations.
/// Enqueue advances each distinct recipient's generation atomically with the message;
/// Skip leaves wake generations unchanged.
/// </summary>
internal enum MailWakePolicy
{
    Skip,
    Enqueue
}
