using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Sorting;

public class ElasticSearchSortProvider : SortProvider<ElasticSearchSortVisitorContext>
{
    /// <inheritdoc/>
    public ElasticSearchSortProvider(
        Action<ISortProviderDescriptor<ElasticSearchSortVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor thar will traverse a incoming query and execute the sorting handlers
    /// </summary>
    protected virtual SortVisitor<ElasticSearchSortVisitorContext, ElasticSearchSortOperation>
        Visitor
    { get; } = new();

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            ElasticSearchSortVisitorContext? visitorContext = null;
            var argument = context.Selection.Field.Arguments[argumentName];
            IValueNode sort = context.ArgumentLiteral<IValueNode>(argumentName);

            if (argument.Type.ElementType().NamedType() is ISortInputType sortInputType)
            {
                visitorContext = new ElasticSearchSortVisitorContext(sortInputType);

                Visitor.Visit(sort, visitorContext);

                await next(context).ConfigureAwait(false);

                if (context.Result is IElasticSearchExecutable executable)
                {
                    executable.WithSorting(visitorContext.Operations.ToArray());
                }
            }
            else
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }
}
