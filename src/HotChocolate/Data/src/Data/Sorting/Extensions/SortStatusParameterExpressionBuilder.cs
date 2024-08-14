using HotChocolate.Data.Sorting;
using HotChocolate.Internal;

namespace HotChocolate.Types;

internal sealed class SortStatusParameterExpressionBuilder()
    : CustomParameterExpressionBuilder<SortStatus>(
        ctx => ctx.GetLocalState<SortStatus>("sortStatus"))
{
    public static SortStatusParameterExpressionBuilder Instance { get; } = new();
}
