#if NET6_0_OR_GREATER
#nullable enable

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Pagination;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Projections;

/// <summary>
/// Provides extension methods for projection on DataLoader.
/// </summary>
#if NET8_0_OR_GREATER
[Experimental(Experiments.Projections)]
#endif
public static class HotChocolateExecutionDataLoaderExtensions
{
    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the selection.
    /// </returns>
    public static ISelectionDataLoader<TKey, TValue> Select<TKey, TValue>(
        this IDataLoader<TKey, TValue> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        var expression = selection.AsSelector<TValue>();
        return dataLoader.Select(expression);
    }

    /// <summary>
    /// Selects the fields that where selected in the GraphQL selection tree.
    /// </summary>
    /// <param name="dataLoader">
    /// The data loader.
    /// </param>
    /// <param name="selection">
    /// The selection that shall be applied to the data loader.
    /// </param>
    /// <typeparam name="TKey">
    /// The key type.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The value type.
    /// </typeparam>
    /// <returns>
    /// Returns a new data loader that applies the selection.
    /// </returns>
    public static IPagingDataLoader<TKey, Page<TValue>> Select<TKey, TValue>(
        this IPagingDataLoader<TKey, Page<TValue>> dataLoader,
        ISelection selection)
        where TKey : notnull
    {
        var flags = ((ObjectField)selection.Field).Flags;

        if ((flags & FieldFlags.Connection) == FieldFlags.Connection)
        {
            var buffer = ArrayPool<ISelection>.Shared.Rent(16);
            var count = GetConnectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                var expression = buffer[i].AsSelector<TValue>();
                dataLoader.Select(expression);
            }
            ArrayPool<ISelection>.Shared.Return(buffer);
        }
        else if ((flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment)
        {
            var buffer = ArrayPool<ISelection>.Shared.Rent(16);
            var count = GetCollectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                var expression = buffer[i].AsSelector<TValue>();
                dataLoader.Select(expression);
            }
            ArrayPool<ISelection>.Shared.Return(buffer);
        }
        else
        {
            var expression = selection.AsSelector<TValue>();
            dataLoader.Select(expression);
        }

        return dataLoader;
    }

    private static int GetConnectionSelections(ISelection selection, Span<ISelection> buffer)
    {
        var pageType = (ObjectType)selection.Field.Type.NamedType();
        var connectionSelections = selection.DeclaringOperation.GetSelectionSet(selection, pageType);
        var count = 0;

        foreach (var connectionChild in connectionSelections.Selections)
        {
            if (connectionChild.Field.Name.EqualsOrdinal("nodes"))
            {
                if (buffer.Length == count)
                {
                    throw new InvalidOperationException("To many alias selections of nodes and edges.");
                }

                buffer[count++] = connectionChild;
            }
            else if (connectionChild.Field.Name.EqualsOrdinal("edges"))
            {
                var edgeType = (ObjectType)selection.Field.Type.NamedType();
                var edgeSelections = selection.DeclaringOperation.GetSelectionSet(connectionChild, edgeType);

                foreach (var edgeChild in edgeSelections.Selections)
                {
                    if (edgeChild.Field.Name.EqualsOrdinal("node"))
                    {
                        if (buffer.Length == count)
                        {
                            throw new InvalidOperationException("To many alias selections of nodes and edges.");
                        }

                        buffer[count++] = edgeChild;
                    }
                }
            }
        }

        return count;
    }

    private static int GetCollectionSelections(ISelection selection, Span<ISelection> buffer)
    {
        var pageType = (ObjectType)selection.Field.Type.NamedType();
        var connectionSelections = selection.DeclaringOperation.GetSelectionSet(selection, pageType);
        var count = 0;

        foreach (var connectionChild in connectionSelections.Selections)
        {
            if (connectionChild.Field.Name.EqualsOrdinal("items"))
            {
                if (buffer.Length == count)
                {
                    throw new InvalidOperationException("To many alias selections of items.");
                }

                buffer[count++] = connectionChild;
            }
        }

        return count;
    }
}
#endif
