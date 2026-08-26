using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// Shared parsing and resolution of the <c>--body</c> / <c>--body-file</c>
/// pair used by every mail command that writes a message.
/// </summary>
internal static class MailBody
{
    /// <summary>
    /// Adds a parse-time validator that requires exactly one of
    /// <c>--body</c> or <c>--body-file</c>.
    /// </summary>
    public static void AddValidator(Command command)
    {
        command.Validators.Add(result =>
        {
            var bodyResult = result.GetResult(Opt<MailBodyOption>.Instance);
            var bodyFileResult = result.GetResult(Opt<MailBodyFileOption>.Instance);

            var hasBody = bodyResult is { Implicit: false };
            var hasBodyFile = bodyFileResult is { Implicit: false };

            if (hasBody == hasBodyFile)
            {
                result.AddError("Exactly one of '--body' or '--body-file' is required.");
            }
        });
    }

    /// <summary>
    /// Resolves the message body from <c>--body</c> or, when given,
    /// verbatim from the file named by <c>--body-file</c>. Throws
    /// <see cref="ExitException"/> when the resolved body is empty.
    /// </summary>
    public static async Task<string> ResolveAsync(
        ParseResult parseResult,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var body = parseResult.GetValue(Opt<MailBodyOption>.Instance);

        if (body is not null)
        {
            if (body.Length is 0)
            {
                throw new ExitException("The '--body' option must not be empty.");
            }

            return body;
        }

        var bodyFile = parseResult.GetRequiredValue(Opt<MailBodyFileOption>.Instance);

        var bodyFilePath = Path.IsPathRooted(bodyFile)
            ? bodyFile
            : Path.Combine(fileSystem.GetCurrentDirectory(), bodyFile);

        if (!fileSystem.FileExists(bodyFilePath))
        {
            throw new ExitException($"The file '{bodyFile}' does not exist.");
        }

        var content = await fileSystem.ReadAllTextAsync(bodyFilePath, cancellationToken);

        if (content.Length is 0)
        {
            throw new ExitException($"The file '{bodyFile}' is empty.");
        }

        return content;
    }
}
