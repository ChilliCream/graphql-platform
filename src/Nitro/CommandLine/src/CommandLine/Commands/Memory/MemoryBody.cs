using ChilliCream.Nitro.CommandLine.Commands.Memory.Options;
using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

/// <summary>
/// Shared parsing and resolution of the text argument / <c>--file</c> pair
/// used by <c>save</c>, mirroring <c>MailBody</c>.
/// </summary>
internal static class MemoryBody
{
    /// <summary>
    /// Adds a parse-time validator that requires exactly one of the text
    /// argument or <c>--file</c>.
    /// </summary>
    public static void AddValidator(Command command)
    {
        command.Validators.Add(result =>
        {
            var textResult = result.GetResult(Opt<MemoryTextArgument>.Instance);
            var fileResult = result.GetResult(Opt<MemoryFileOption>.Instance);

            var hasText = textResult is { Implicit: false };
            var hasFile = fileResult is { Implicit: false };

            if (hasText == hasFile)
            {
                result.AddError("Exactly one of the text argument or '--file' is required.");
            }
        });
    }

    /// <summary>
    /// Resolves the memory text from the positional argument or, when
    /// given, verbatim from the file named by <c>--file</c>. Throws
    /// <see cref="ExitException"/> when the resolved text is empty.
    /// </summary>
    public static async Task<string> ResolveAsync(
        ParseResult parseResult,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var text = parseResult.GetValue(Opt<MemoryTextArgument>.Instance);

        if (text is not null)
        {
            if (text.Length is 0)
            {
                throw new ExitException("The text argument must not be empty.");
            }

            return text;
        }

        var file = parseResult.GetRequiredValue(Opt<MemoryFileOption>.Instance);

        return await ReadFileAsync(fileSystem, file, cancellationToken);
    }

    /// <summary>
    /// Reads a file's content, resolving a relative path against the
    /// current directory. Line endings are normalized to LF and leading
    /// blank lines are dropped. Throws <see cref="ExitException"/> when the
    /// file does not exist or is empty.
    /// </summary>
    public static async Task<string> ReadFileAsync(
        IFileSystem fileSystem,
        string file,
        CancellationToken cancellationToken)
    {
        var filePath = Path.IsPathRooted(file)
            ? file
            : Path.Combine(fileSystem.GetCurrentDirectory(), file);

        if (!fileSystem.FileExists(filePath))
        {
            throw new ExitException($"The file '{file}' does not exist.");
        }

        var content = await fileSystem.ReadAllTextAsync(filePath, cancellationToken);
        content = content.Replace("\r\n", "\n").TrimStart('\n');

        if (content.Length is 0)
        {
            throw new ExitException($"The file '{file}' is empty.");
        }

        return content;
    }
}
