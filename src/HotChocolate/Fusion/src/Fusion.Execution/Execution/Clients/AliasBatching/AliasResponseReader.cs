using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Splits a single alias batched response back into per-row results. Each alias key in the
/// merged response is correlated to the inbound request and row it came from, and a small
/// independent result document is built for that row with the original field names restored.
/// </summary>
/// <remarks>
/// The per-row documents are independent of the merged response document. The merged document is
/// disposed once the per-row documents have been built, while the per-row documents live on with
/// the results they back.
/// </remarks>
internal static class AliasResponseReader
{
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;
    private static ReadOnlySpan<byte> PathProperty => "path"u8;

    /// <summary>
    /// Reads the alias batched HTTP response and yields a per-row result for every populated slot,
    /// in (operation, row) order.
    /// </summary>
    /// <param name="response">The alias batched HTTP response.</param>
    /// <param name="arena">The memory arena that owns parsed source result documents.</param>
    /// <param name="requests">The inbound requests the batch was merged from.</param>
    /// <param name="batched">The rewritten alias batched operation describing the merge.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The per-row results in (operation, row) order.</returns>
    public static async IAsyncEnumerable<AliasRowResult> ReadAsync(
        GraphQLHttpResponse response,
        IMemoryArena arena,
        ImmutableArray<SourceSchemaClientRequest> requests,
        AliasBatchedOperation batched,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(arena);

        var merged = await response.ReadAsResultAsync(arena, cancellationToken).ConfigureAwait(false);

        // The per-row documents are built eagerly so the merged document can be disposed
        // immediately. Each per-row result then owns an independent document.
        var results = Split(merged, arena, requests, batched);

        foreach (var result in results)
        {
            yield return result;
        }
    }

    /// <summary>
    /// Splits the merged response document into independent per-row results. The merged document
    /// is disposed before this method returns; the returned results own their own documents.
    /// </summary>
    /// <param name="merged">The merged alias batched response document.</param>
    /// <param name="arena">The memory arena that owns parsed source result documents.</param>
    /// <param name="requests">The inbound requests the batch was merged from.</param>
    /// <param name="batched">The rewritten alias batched operation describing the merge.</param>
    /// <returns>The per-row results in (operation, row) order.</returns>
    public static List<AliasRowResult> Split(
        SourceResultDocument merged,
        IMemoryArena arena,
        ImmutableArray<SourceSchemaClientRequest> requests,
        AliasBatchedOperation batched)
    {
        ArgumentNullException.ThrowIfNull(merged);
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(batched);

        try
        {
            var prefixes = batched.Prefixes;
            var aliasToRoot = BuildAliasLookup(prefixes);

            merged.Root.TryGetProperty(DataProperty, out var data);
            var hasData = data.ValueKind == JsonValueKind.Object;

            merged.Root.TryGetProperty(ErrorsProperty, out var errors);
            var hasErrors = errors.ValueKind == JsonValueKind.Array;

            var slots = BuildSlots(prefixes, batched.RootResponseNames);

            if (hasErrors)
            {
                RouteErrors(errors, aliasToRoot, slots);
            }

            var results = new List<AliasRowResult>(slots.Length);

            foreach (var slot in slots)
            {
                var result = TryBuildRowResult(slot, arena, requests, data, hasData);

                if (result is not null)
                {
                    results.Add(result.Value);
                }
            }

            return results;
        }
        finally
        {
            merged.Dispose();
        }
    }

    private static Dictionary<string, int> BuildAliasLookup(AliasPrefixTable prefixes)
    {
        var lookup = new Dictionary<string, int>(prefixes.RootCount, StringComparer.Ordinal);

        for (var i = 0; i < prefixes.RootCount; i++)
        {
            lookup[prefixes.RootAliases[i]] = i;
        }

        return lookup;
    }

    private static Slot[] BuildSlots(AliasPrefixTable prefixes, ImmutableArray<string> rootResponseNames)
    {
        // Roots are emitted in (operation, row) order, so consecutive roots sharing the same
        // (operation, row) belong to one slot. Multi-root operations contribute several roots.
        var slots = new List<Slot>();
        var index = 0;
        var rootCount = prefixes.RootCount;

        while (index < rootCount)
        {
            var operationIndex = prefixes.RootOperationIndices[index];
            var rowIndex = prefixes.RootRowIndices[index];

            var roots = new List<RootRef>();

            while (index < rootCount
                && prefixes.RootOperationIndices[index] == operationIndex
                && prefixes.RootRowIndices[index] == rowIndex)
            {
                roots.Add(new RootRef(prefixes.RootAliases[index], rootResponseNames[index]));
                index++;
            }

            slots.Add(new Slot(operationIndex, rowIndex, roots));
        }

        return slots.ToArray();
    }

    private static void RouteErrors(
        SourceResultElement errors,
        Dictionary<string, int> aliasToRoot,
        Slot[] slots)
    {
        // Map an alias directly to the slot it routes to and the original response name that
        // replaces the alias in the per-row error path.
        var aliasToTarget = new Dictionary<string, ErrorTarget>(StringComparer.Ordinal);

        foreach (var slot in slots)
        {
            foreach (var root in slot.Roots)
            {
                aliasToTarget[root.Alias] = new ErrorTarget(slot, root.ResponseName);
            }
        }

        foreach (var error in errors.EnumerateArray())
        {
            if (error.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (TryGetErrorPathAlias(error, aliasToRoot, out var alias)
                && aliasToTarget.TryGetValue(alias, out var target))
            {
                target.Slot.Errors.Add(new RoutedError(error, target.ResponseName));
            }
            else
            {
                // The error has no path, or its path does not start with a known alias.
                // Broadcast it to every row so it is not silently dropped.
                foreach (var slot in slots)
                {
                    slot.Errors.Add(new RoutedError(error, OriginalResponseName: null));
                }
            }
        }
    }

    private static bool TryGetErrorPathAlias(
        SourceResultElement error,
        Dictionary<string, int> aliasToRoot,
        out string alias)
    {
        alias = string.Empty;

        if (!error.TryGetProperty(PathProperty, out var path)
            || path.ValueKind != JsonValueKind.Array
            || path.GetArrayLength() == 0)
        {
            return false;
        }

        var first = path[0];

        if (first.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = first.GetString();

        if (value is null || !aliasToRoot.ContainsKey(value))
        {
            return false;
        }

        alias = value;
        return true;
    }

    private static AliasRowResult? TryBuildRowResult(
        Slot slot,
        IMemoryArena arena,
        ImmutableArray<SourceSchemaClientRequest> requests,
        SourceResultElement data,
        bool hasData)
    {
        var presentRoots = new List<PresentRoot>(slot.Roots.Count);

        if (hasData)
        {
            foreach (var root in slot.Roots)
            {
                if (data.TryGetProperty(root.Alias, out var value))
                {
                    presentRoots.Add(new PresentRoot(root.ResponseName, value));
                }
            }
        }

        // A slot is emitted when it carries data or at least one error. A slot with neither is
        // skipped, so the downstream batch node can raise its missing-result diagnostic.
        if (presentRoots.Count == 0 && slot.Errors.Count == 0)
        {
            return null;
        }

        var document = BuildRowDocument(arena, presentRoots, slot.Errors);

        var request = requests[slot.OperationIndex];
        var path = CompactPath.Root;
        var additionalPaths = default(CompactPathSegment);

        if (slot.RowIndex < request.Variables.Length)
        {
            var variable = request.Variables[slot.RowIndex];
            path = variable.Path;
            additionalPaths = variable.AdditionalPaths;
        }

        var result = additionalPaths.IsDefaultOrEmpty
            ? new SourceSchemaResult(path, document)
            : new SourceSchemaResult(path, document, additionalPaths: additionalPaths);

        return new AliasRowResult(slot.OperationIndex, result);
    }

    private static SourceResultDocument BuildRowDocument(
        IMemoryArena arena,
        List<PresentRoot> roots,
        List<RoutedError> errors)
    {
        using var buffer = new PooledArrayWriter();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            writer.WritePropertyName(DataProperty);

            if (roots.Count == 0)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStartObject();

                foreach (var root in roots)
                {
                    writer.WritePropertyName(root.ResponseName);
                    writer.WriteRawValue(root.Value.GetRawValue(), skipInputValidation: true);
                }

                writer.WriteEndObject();
            }

            if (errors.Count > 0)
            {
                writer.WritePropertyName(ErrorsProperty);
                writer.WriteStartArray();

                foreach (var error in errors)
                {
                    WriteRoutedError(writer, error);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        var written = buffer.WrittenSpan;
        var bytes = written.ToArray();

        return SourceResultDocument.Parse(arena, bytes, bytes.Length);
    }

    private static void WriteRoutedError(Utf8JsonWriter writer, RoutedError routedError)
    {
        var error = routedError.Error;

        // A routed error (path[0] is a known alias) has its leading alias segment replaced by the
        // original response name, so the per-row path looks as if the row had executed alone. A
        // broadcast error is copied verbatim.
        if (routedError.OriginalResponseName is null)
        {
            error.WriteRawValueTo(writer);
            return;
        }

        writer.WriteStartObject();

        foreach (var property in error.EnumerateObject())
        {
            if (property.NameEquals(PathProperty))
            {
                writer.WritePropertyName(PathProperty);
                WriteRemappedPath(writer, property.Value, routedError.OriginalResponseName);
                continue;
            }

            writer.WritePropertyName(property.NameSpan);
            property.Value.WriteRawValueTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteRemappedPath(
        Utf8JsonWriter writer,
        SourceResultElement path,
        string originalResponseName)
    {
        writer.WriteStartArray();

        var first = true;

        foreach (var segment in path.EnumerateArray())
        {
            if (first)
            {
                // Replace the alias segment with the original response name.
                writer.WriteStringValue(originalResponseName);
                first = false;
                continue;
            }

            segment.WriteRawValueTo(writer);
        }

        writer.WriteEndArray();
    }

    private static void WriteRawValueTo(this SourceResultElement element, Utf8JsonWriter writer)
        => writer.WriteRawValue(element.GetRawValue(), skipInputValidation: true);

    private readonly record struct RootRef(string Alias, string ResponseName);

    private readonly record struct PresentRoot(string ResponseName, SourceResultElement Value);

    private readonly record struct ErrorTarget(Slot Slot, string ResponseName);

    private sealed class Slot(int operationIndex, int rowIndex, List<RootRef> roots)
    {
        public int OperationIndex { get; } = operationIndex;

        public int RowIndex { get; } = rowIndex;

        public List<RootRef> Roots { get; } = roots;

        public List<RoutedError> Errors { get; } = [];
    }

    private readonly record struct RoutedError(SourceResultElement Error, string? OriginalResponseName);
}
