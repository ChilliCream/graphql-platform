using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Pagination;

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
        => MapArguments(context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments));

    private static PagingArguments MapArguments(CursorPagingArguments arguments)
        => new(arguments.First, arguments.After, arguments.Last, arguments.Before);
}
