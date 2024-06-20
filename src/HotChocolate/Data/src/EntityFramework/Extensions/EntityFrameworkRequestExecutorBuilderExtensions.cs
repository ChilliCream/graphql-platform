using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;
using HotChocolate.Internal;
using HotChocolate.Resolvers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class EntityFrameworkRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds resolver compiler mapping for the <see cref="PagingArguments"/> from the EFCore helper lib.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddPagingArguments(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder, PagingArgumentsParameterExpressionBuilder>();
        return builder;
    }

    private sealed class PagingArgumentsParameterExpressionBuilder()
        : CustomParameterExpressionBuilder<PagingArguments>(ctx => MapArguments(ctx))
        , IParameterBindingFactory
        , IParameterBinding
    {
        public IParameterBinding Create(ParameterBindingContext context)
            => this;

        public ArgumentKind Kind => ArgumentKind.Custom;

        public T Execute<T>(IResolverContext context)
            => (T)(object)MapArguments(context);

        public T Execute<T>(IPureResolverContext context)
            => throw new NotSupportedException();

        private static PagingArguments MapArguments(IResolverContext context)
            => MapArguments(context.GetLocalState<CursorPagingArguments>(WellKnownContextData.PagingArguments));

        private static PagingArguments MapArguments(CursorPagingArguments arguments)
            => new(arguments.First, arguments.After, arguments.Last, arguments.Before);
    }
}
