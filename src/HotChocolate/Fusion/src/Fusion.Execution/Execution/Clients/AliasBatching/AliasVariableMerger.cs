using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Writes the merged variables object for an alias batched operation. Every inbound variable
/// row contributes its values under the prefixed names the rewriter assigned, so the single
/// merged document can reference each row's variables without collisions.
/// </summary>
internal static class AliasVariableMerger
{
    /// <summary>
    /// Writes a single JSON object containing every prefixed variable for the batch.
    /// </summary>
    /// <param name="writer">The writer that receives the merged variables object.</param>
    /// <param name="prefixes">The prefix table describing the original to prefixed name mapping.</param>
    /// <param name="requests">The inbound requests holding the original variable values.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a variable declared by the operation is missing from its row's values.
    /// </exception>
    public static void Write(
        Utf8JsonWriter writer,
        AliasPrefixTable prefixes,
        ImmutableArray<SourceSchemaClientRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(prefixes);

        writer.WriteStartObject();

        var count = prefixes.VariableCount;
        var index = 0;

        // The variable group is ordered so that all entries for one (operation, row) form a
        // contiguous run. Each run is satisfied from a single parse of that row's values.
        while (index < count)
        {
            var operationIndex = prefixes.VariableOperationIndices[index];
            var rowIndex = prefixes.VariableRowIndices[index];

            var runEnd = index;
            while (runEnd < count
                && prefixes.VariableOperationIndices[runEnd] == operationIndex
                && prefixes.VariableRowIndices[runEnd] == rowIndex)
            {
                runEnd++;
            }

            WriteRow(writer, prefixes, requests, operationIndex, rowIndex, index, runEnd);
            index = runEnd;
        }

        writer.WriteEndObject();
    }

    private static void WriteRow(
        Utf8JsonWriter writer,
        AliasPrefixTable prefixes,
        ImmutableArray<SourceSchemaClientRequest> requests,
        int operationIndex,
        int rowIndex,
        int runStart,
        int runEnd)
    {
        var request = requests[operationIndex];
        var values = rowIndex < request.Variables.Length
            ? request.Variables[rowIndex].Values
            : JsonSegment.Empty;

        if (values.IsEmpty)
        {
            ThrowMissingVariable(prefixes.OriginalVariableNames[runStart]);
        }

        using var document = JsonDocument.Parse(values.AsSequence());
        var root = document.RootElement;

        for (var i = runStart; i < runEnd; i++)
        {
            var originalName = prefixes.OriginalVariableNames[i];
            var prefixedName = prefixes.PrefixedVariableNames[i];

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(originalName, out var value))
            {
                ThrowMissingVariable(originalName);
            }
            else
            {
                writer.WritePropertyName(prefixedName);
                value.WriteTo(writer);
            }
        }
    }

    private static void ThrowMissingVariable(string originalName)
        => throw new InvalidOperationException(
            $"The variable '${originalName}' was declared by the operation but is missing "
            + "from its variable values, so the alias batched request cannot be built.");
}
