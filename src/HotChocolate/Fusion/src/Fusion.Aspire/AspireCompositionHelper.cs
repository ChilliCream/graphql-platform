using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Logging;

namespace HotChocolate.Fusion.Aspire;

internal static class AspireCompositionHelper
{
    public static async Task<bool> TryComposeAsync(
        string fusionArchivePath,
        ImmutableArray<SourceSchemaInfo> newSourceSchemas,
        GraphQLCompositionSettings settings,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        using var archive = File.Exists(fusionArchivePath)
            ? FusionArchive.Open(fusionArchivePath, FusionArchiveMode.Update)
            : FusionArchive.Create(fusionArchivePath);

        var compositionLog = new CompositionLog();
        var environment = settings.EnvironmentName ?? "Aspire";
        var compositionSettings = new CompositionSettings
        {
            Merger = { EnableGlobalObjectIdentification = settings.EnableGlobalObjectIdentification }
        };
        var sourceSchemas = newSourceSchemas.ToDictionary(
            s => s.Name,
            s => (s.Schema, s.SchemaSettings));

        var result = await CompositionHelper.ComposeAsync(
            compositionLog,
            sourceSchemas,
            archive,
            environment,
            compositionSettings,
            cancellationToken);

        var output = new StringBuilder();

        foreach (var entry in compositionLog)
        {
            if (entry.Severity is LogSeverity.Error)
            {
                output.AppendLine($"‼️ {FormatMultilineMessage(entry.Message)}");
            }
            else
            {
                output.AppendLine(entry.Message);
            }
        }

        if (result.IsFailure)
        {
            output.Append("❌ Composition failed:");
            logger.LogError("{Message}", output.ToString());
            return false;
        }

        output.Append("✅ Composition completed successfully.");
        logger.LogInformation("{Message}", output.ToString());

        return true;
    }

    /// <summary>
    /// Since we're prefixing the message with an emoji and space before printing,
    /// we need to also indent each line of a multiline message by three spaces to fix the alignment.
    /// </summary>
    private static string FormatMultilineMessage(string message)
    {
        var lines = message.Split(Environment.NewLine);

        if (lines.Length <= 1)
        {
            return message;
        }

        return string.Join(Environment.NewLine + "   ", lines);
    }
}
