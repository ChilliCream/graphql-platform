

using System.Buffers.Text;
using System.Linq.Expressions;
using System.Text;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Projections;

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
    public static Expression<Func<TValue, TValue>> ToSelectorExpression<TValue>(
        this ISelection selection)
        => GetOrCreateExpression<TValue>(selection);

    private static Expression<Func<TValue, TValue>> GetOrCreateExpression<TValue>(
        ISelection selection)
    {
        return selection.DeclaringOperation.GetOrAddState(
            CreateExpressionKey(selection.Id),
            static (_, ctx) => ctx._builder.BuildExpression<TValue>(ctx.selection),
            (_builder, selection));
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
        var length = (value < 0) ? 1 : 0;

        // we add the number of digits the number has to the length of the number.
        length += (int)Math.Floor(Math.Log10(Math.Abs(value)) + 1);

        return length;
    }
}
