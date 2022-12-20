using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a filter definition
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
        Visitor
    { get; } = new(new ElasticSearchFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(nameof(IElasticQueryFactory),
                    new ElasticQueryFactory(this, argumentName));
            await next(context).ConfigureAwait(false);
        }
    }

    public override IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        if (!fieldDefinition.ContextData
                .TryGetValue(nameof(ElasticFilterMetadata), out object? metadata))
        {
            return null;
        }

        fieldDefinition.ContextData.Remove(nameof(ElasticFilterMetadata));

        return metadata as ElasticFilterMetadata;
    }

    private class ElasticQueryFactory : IElasticQueryFactory
    {
        private readonly ElasticSearchFilterProvider _provider;
        private readonly string _argumentName;

        public ElasticQueryFactory(
            ElasticSearchFilterProvider provider,
            string argumentName)
        {
            _provider = provider;
            _argumentName = argumentName;
        }

        public BoolOperation? Create(
            IResolverContext context,
            IAbstractElasticClient client)
        {
            ElasticSearchFilterVisitorContext? visitorContext = null;
            IInputField argument = context.Selection.Field.Arguments[_argumentName];
            IValueNode filter = context.ArgumentLiteral<IValueNode>(_argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                visitorContext = new ElasticSearchFilterVisitorContext(filterInput, client);

                _provider.Visitor.Visit(filter, visitorContext);

                if (!visitorContext.TryCreateQuery(out BoolOperation? whereQuery) ||
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
