using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using System.Runtime.CompilerServices;
using GreenDonut.Selectors;
using HotChocolate.Execution.Projections;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution.Processing;

/// <summary>
/// Provides extension methods to work with selections.
/// </summary>
public static class HotChocolateExecutionSelectionExtensions
{
    private static readonly SelectionExpressionBuilder _builder = new();

    /// <summary>
    /// Creates a selector expression from a GraphQL selection.
    /// </summary>
    /// <param name="selection">
    /// The selection that shall be converted into a selector expression.
    /// </param>
    /// <typeparam name="TValue">
    /// The type of the value that is returned by the <see cref="ISelection"/>.
    /// </typeparam>
    /// <returns>
    /// Returns a selector expression that can be used for data projections.
    /// </returns>
    [Experimental(Experiments.Selectors)]
    public static Expression<Func<TValue, TValue>> AsSelector<TValue>(
        this ISelection selection)
    {
        // we first check if we already have an expression for this selection,
        // this would be the cheapest way to get the expression.
        if(TryGetExpression<TValue>(selection, out var expression))
        {
            return expression;
        }

        // if we do not have an expression we need to create one.
        // we first check what kind of field selection we have,
        // connection, collection or single field.
        var flags = ((ObjectField)selection.Field).Flags;

        if ((flags & FieldFlags.Connection) == FieldFlags.Connection)
        {
            var builder = new DefaultSelectorBuilder();
            var buffer = ArrayPool<ISelection>.Shared.Rent(16);
            var count = GetConnectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                builder.Add(GetOrCreateExpression<TValue>(buffer[i]));
            }
            ArrayPool<ISelection>.Shared.Return(buffer);
            return GetOrCreateExpression<TValue>(selection, builder);
        }

        if ((flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment)
        {
            var builder = new DefaultSelectorBuilder();
            var buffer = ArrayPool<ISelection>.Shared.Rent(16);
            var count = GetCollectionSelections(selection, buffer);
            for (var i = 0; i < count; i++)
            {
                builder.Add(GetOrCreateExpression<TValue>(buffer[i]));
            }
            ArrayPool<ISelection>.Shared.Return(buffer);
            return GetOrCreateExpression<TValue>(selection, builder);
        }

        if ((flags & FieldFlags.GlobalIdNodeField) == FieldFlags.GlobalIdNodeField
            || (flags & FieldFlags.GlobalIdNodeField) == FieldFlags.GlobalIdNodeField)
        {
            return GetOrCreateNodeExpression<TValue>(selection);
        }

        return GetOrCreateExpression<TValue>(selection);
    }

    private static Expression<Func<TValue, TValue>> GetOrCreateExpression<TValue>(
        ISelection selection)
        => selection.DeclaringOperation.GetOrAddState(
            CreateExpressionKey(selection.Id),
            static (_, ctx) => ctx._builder.BuildExpression<TValue>(ctx.selection),
            (_builder, selection));

    [Experimental(Experiments.Selectors)]
    private static Expression<Func<TValue, TValue>> GetOrCreateExpression<TValue>(
        ISelection selection,
        ISelectorBuilder builder)
        => selection.DeclaringOperation.GetOrAddState(
            CreateExpressionKey(selection.Id),
            static (_, ctx) => ctx.builder.TryCompile<TValue>()!,
            (builder, selection));

    private static Expression<Func<TValue, TValue>> GetOrCreateNodeExpression<TValue>(
        ISelection selection)
        => selection.DeclaringOperation.GetOrAddState(
            CreateNodeExpressionKey<TValue>(selection.Id),
            static (_, ctx) => ctx._builder.BuildNodeExpression<TValue>(ctx.selection),
            (_builder, selection));

    private static bool TryGetExpression<TValue>(
        ISelection selection,
        [NotNullWhen(true)] out Expression<Func<TValue, TValue>>? expression)
        => selection.DeclaringOperation.TryGetState(CreateExpressionKey(selection.Id), out expression);

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
                var edgeType = (ObjectType)connectionChild.Field.Type.NamedType();
                var edgeSelections = connectionChild.DeclaringOperation.GetSelectionSet(connectionChild, edgeType);

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

    private static string CreateExpressionKey(int key)
    {
        var keyPrefix = GetKeyPrefix();
        var requiredBufferSize = EstimateIntLength(key) + keyPrefix.Length;
        Span<byte> span = stackalloc byte[requiredBufferSize];
        keyPrefix.CopyTo(span);
        Utf8Formatter.TryFormat(key, span.Slice(keyPrefix.Length), out var written, 'D');
        return Encoding.UTF8.GetString(span.Slice(0, written + keyPrefix.Length));
    }

    private static string CreateNodeExpressionKey<TValue>(int key)
    {
        var typeName = typeof(TValue).FullName!;
        var typeNameLength = Encoding.UTF8.GetMaxByteCount(typeName.Length);
        var keyPrefix = GetKeyPrefix();
        var requiredBufferSize = EstimateIntLength(key) + keyPrefix.Length + typeNameLength;
        byte[]? rented = null;
        var span =  requiredBufferSize <= 256
            ? stackalloc byte[requiredBufferSize]
            : (rented = ArrayPool<byte>.Shared.Rent(requiredBufferSize));

        keyPrefix.CopyTo(span);
        Utf8Formatter.TryFormat(key, span.Slice(keyPrefix.Length), out var written, 'D');
        var typeNameWritten = Encoding.UTF8.GetBytes(typeName, span.Slice(written + keyPrefix.Length));
        var keyString = Encoding.UTF8.GetString(span.Slice(0, written + keyPrefix.Length + typeNameWritten));

        if (rented is not null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return keyString;
    }

    private static ReadOnlySpan<byte> GetKeyPrefix()
        => "hc-dataloader-expr-"u8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateIntLength(int value)
    {
        if (value == 0)
        {
            // to print 0 we need still 1 digit
            return 1;
        }

        // if the number is negative we need one more digit for the sign
        var length = value < 0 ? 1 : 0;

        // we add the number of digits the number has to the length of the number.
        length += (int)Math.Floor(Math.Log10(Math.Abs(value)) + 1);

        return length;
    }
}
