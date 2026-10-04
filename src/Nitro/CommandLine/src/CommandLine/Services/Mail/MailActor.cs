namespace ChilliCream.Nitro.CommandLine.Services.Mail;

using ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the acting agent address recorded on mail mutations.
/// </summary>
internal static class MailActor
{
    public static Task<string> ResolveAsync(
        string? optionValue,
        IActingActorResolver resolver,
        CancellationToken cancellationToken)
        => resolver.ResolveAsync(optionValue, cancellationToken);
}
