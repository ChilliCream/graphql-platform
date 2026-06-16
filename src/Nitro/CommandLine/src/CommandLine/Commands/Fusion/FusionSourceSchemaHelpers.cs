using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Packaging;

namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal static class FusionSourceSchemaHelpers
{
    public static string ResolveExistingArchiveFile(IFileSystem fileSystem, string archiveFile)
    {
        if (!Path.IsPathRooted(archiveFile))
        {
            archiveFile = Path.Combine(fileSystem.GetCurrentDirectory(), archiveFile);
        }

        if (!fileSystem.FileExists(archiveFile))
        {
            throw new ExitException(Messages.ArchiveFileDoesNotExist(archiveFile));
        }

        return archiveFile;
    }

    /// <summary>
    /// Applies a mutation to an in-memory copy of the archive, recomposes it, and only writes
    /// the result back to disk when the recomposition succeeds. The on-disk archive is left
    /// untouched when the mutation or the recomposition fails.
    /// </summary>
    public static async Task<int> ApplyAndRecomposeAsync(
        string archiveFile,
        Func<FusionArchive, Task> mutateAsync,
        Dictionary<string, (SourceSchemaText, JsonDocument)> newSourceSchemas,
        string environment,
        INitroConsole console,
        CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(archiveFile, cancellationToken);

        var buffer = new MemoryStream();
        buffer.Write(bytes, 0, bytes.Length);
        buffer.Position = 0;

        using var archive = FusionArchive.Open(buffer, FusionArchiveMode.Update, leaveOpen: true);

        await mutateAsync(archive);

        await using var composeActivity = console.StartActivity(
            "Composing new configuration",
            "Failed to compose new configuration.");

        var (result, compositionLog) = await FusionPublishHelpers.ComposeAsync(
            archive,
            environment,
            newSourceSchemas,
            compositionSettings: null,
            legacyArchive: null,
            cancellationToken);

        if (result.IsSuccess)
        {
            buffer.Position = 0;
            await File.WriteAllBytesAsync(archiveFile, buffer.ToArray(), cancellationToken);

            composeActivity.Success("Composed new configuration.");

            return ExitCodes.Success;
        }
        else
        {
            await composeActivity.FailAllAsync();

            console.WriteLine();
            console.WriteLine("## Composition log");
            console.WriteLine();

            FusionComposeCommand.WriteCompositionLog(
                compositionLog,
                console.Out,
                false);

            foreach (var error in result.Errors)
            {
                console.Error.WriteErrorLine(error.Message);
            }

            throw new ExitException();
        }
    }
}
