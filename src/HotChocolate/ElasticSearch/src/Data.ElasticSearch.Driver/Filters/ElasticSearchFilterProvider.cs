using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

public interface IAbstractElasticClient
{
    string GetName(IFilterField field);
}

public interface IElasticQueryFactory
{
    QueryDefinition? Create( IResolverContext context, IAbstractElasticClient client);
}

internal class ElasticQueryFactory : IElasticQueryFactory
{

    private readonly CreateElasticQuery _createElasticQuery;

    public ElasticQueryFactory(CreateElasticQuery createElasticQuery)
    {
        _createElasticQuery = createElasticQuery;
    }

    public QueryDefinition? Create(
        IResolverContext context,
        IAbstractElasticClient client)
        => _createElasticQuery(context, client);
}

internal delegate QueryDefinition? CreateElasticQuery(
    IResolverContext context,
    IAbstractElasticClient client);

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a
/// <see cref="FilterDefinition{T}"/>
/// </summary>
public class ElasticSearchFilterProvider
    : FilterProvider<ElasticSearchFilterVisitorContext>
{
    /// <inheritdoc />
    public ElasticSearchFilterProvider()
    {
    }

    /// <inheritdoc />
    public ElasticSearchFilterProvider(
        Action<IFilterProviderDescriptor<ElasticSearchFilterVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to traverse the incoming selection set an execute handlers
    /// </summary>
    protected virtual FilterVisitor<ElasticSearchFilterVisitorContext, ISearchOperation>
        Visitor { get; } = new(new ElasticSearchFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(nameof(IElasticQueryFactory), new ElasticQueryFactory(CreateQuery));
            await next(context).ConfigureAwait(false);
        }

        QueryDefinition? CreateQuery(
            IResolverContext context,
            IAbstractElasticClient client)
        {
            ElasticSearchFilterVisitorContext? visitorContext = null;
            IInputField argument = context.Selection.Field.Arguments[argumentName];
            IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                visitorContext = new ElasticSearchFilterVisitorContext(filterInput, client);

                Visitor.Visit(filter, visitorContext);

                if (!visitorContext.TryCreateQuery(out QueryDefinition? whereQuery) ||
                    visitorContext.Errors.Count > 0)
                {
                    foreach (IError error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }

                return whereQuery;
            }

            return null;
        }

    }

}

