using System.Runtime.InteropServices;
using System.Text.Json;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Splits the single response of a batched request into one result per item. Fields and error
/// paths are dispatched by their alias, never by their position, because a source schema is free
/// to order the fields of its response.
/// </summary>
internal static class AliasBatchResponseSplitter
{
    private static ReadOnlySpan<byte> DataProperty => "data"u8;
    private static ReadOnlySpan<byte> ErrorsProperty => "errors"u8;
    private static ReadOnlySpan<byte> PathProperty => "path"u8;

    /// <summary>
    /// Splits <paramref name="document"/> into one result per item of the batched request. The
    /// results share the document, which is freed once the last of them is disposed.
    /// </summary>
    /// <param name="document">The response document of the batched request.</param>
    /// <param name="items">The items of the batched request.</param>
    /// <param name="results">
    /// The buffer that receives one result per item. It must hold at least as many entries as
    /// there are items.
    /// </param>
    /// <returns>How the response relates to the items of the request.</returns>
    public static AliasBatchSplitStatus TrySplit(
        SourceResultDocument document,
        ReadOnlySpan<AliasBatchItem> items,
        SourceSchemaResult[] results)
    {
        document.Root.TryGetProperty(ErrorsProperty, out var errors);

        List<IError>?[]? routedErrors = null;

        if (errors.ValueKind is JsonValueKind.Array)
        {
            var status = RouteErrors(errors, items, ref routedErrors);

            if (status is not AliasBatchSplitStatus.Split)
            {
                return status;
            }
        }

        if (!document.Root.TryGetProperty(DataProperty, out var data)
            || data.ValueKind is not JsonValueKind.Object)
        {
            return AliasBatchSplitStatus.Invalid;
        }

        var roots = new SourceResultElement[items.Length];

        foreach (var property in data.EnumerateObject())
        {
            if (!TryResolveItem(property.NameSpan, items, out var index))
            {
                return AliasBatchSplitStatus.Invalid;
            }

            roots[index] = property.Value;
        }

        var documentOwner = new SourceResultDocumentOwner(document, items.Length);

        for (var i = 0; i < items.Length; i++)
        {
            var variables = items[i].Variables;
            var itemErrors = routedErrors?[i];

            results[i] = new SourceSchemaResult(
                variables.Path,
                document,
                documentOwner,
                roots[i],
                itemErrors is null
                    ? null
                    : SourceSchemaErrors.Create(CollectionsMarshal.AsSpan(itemErrors)),
                variables.AdditionalPaths);
        }

        return AliasBatchSplitStatus.Split;
    }

    /// <summary>
    /// Routes every error to the item its path identifies. An error without a path applies to the
    /// whole request, and an error whose path names no item makes the response unusable.
    /// </summary>
    private static AliasBatchSplitStatus RouteErrors(
        SourceResultElement errors,
        ReadOnlySpan<AliasBatchItem> items,
        ref List<IError>?[]? routedErrors)
    {
        foreach (var error in errors.EnumerateArray())
        {
            if (error.ValueKind is not JsonValueKind.Object
                || !error.TryGetProperty(PathProperty, out var path)
                || path.ValueKind is not JsonValueKind.Array
                || path.GetArrayLength() == 0)
            {
                return AliasBatchSplitStatus.Broadcast;
            }

            var first = path[0];

            if (first.ValueKind is not JsonValueKind.String
                || !TryResolveItem(first.ValueSpan, items, out var index))
            {
                return AliasBatchSplitStatus.Invalid;
            }

            var routed = SourceSchemaErrors.CreateRoutedError(error, items[index].RootField.Name);

            if (routed is null)
            {
                return AliasBatchSplitStatus.Invalid;
            }

            routedErrors ??= new List<IError>?[items.Length];
            (routedErrors[index] ??= []).Add(routed);
        }

        return AliasBatchSplitStatus.Split;
    }

    /// <summary>
    /// Resolves the item that an aliased response name belongs to. The alias must carry the item's
    /// prefix and the response name of that item's root selection.
    /// </summary>
    private static bool TryResolveItem(
        ReadOnlySpan<byte> alias,
        ReadOnlySpan<AliasBatchItem> items,
        out int index)
    {
        if (!AliasBatchPrefixHelper.TryReadIndex(alias, out index, out var name)
            || (uint)index >= (uint)items.Length)
        {
            return false;
        }

        return name.SequenceEqual(items[index].RootField.Utf8Name);
    }
}
