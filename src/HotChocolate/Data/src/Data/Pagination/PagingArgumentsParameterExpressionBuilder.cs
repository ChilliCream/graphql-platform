using GreenDonut.Data;
using HotChocolate.Execution.Processing;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Pagination;

internal sealed class PagingArgumentsParameterExpressionBuilder()
    : CustomParameterExpressionBuilder<PagingArguments>(ctx => MapArguments(ctx))
    , IParameterBindingFactory
    , IParameterBinding
{
    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => false;

    public T Execute<T>(IResolverContext context)
        => (T)(object)MapArguments(context);

    private static PagingArguments MapArguments(IResolverContext context)
    {
        var pagingOptions = PagingHelper.GetPagingOptions(context.Schema, context.Selection.Field);
        var pagingArguments = context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments);
        var includeTotalCount = IncludeTotalCount(context.Selection);

        if (includeTotalCount)
        {
            includeTotalCount = context.IsSelected("totalCount");
        }

        return MapArguments(pagingArguments, includeTotalCount, pagingOptions.NullOrdering);
    }

    private static PagingArguments MapArguments(
        CursorPagingArguments arguments,
        bool includeTotalCount,
        NullOrdering nullOrdering)
        => new(
            arguments.First,
            arguments.After,
            arguments.Last,
            arguments.Before,
            includeTotalCount)
        { NullOrdering = nullOrdering };

    private static bool IncludeTotalCount(ISelection selection)
        => selection.Field.Features.Get<PagingOptions>()?.IncludeTotalCount is true;
}
