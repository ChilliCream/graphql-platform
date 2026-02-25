using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data.ElasticSearch.Attributes;
using HotChocolate.Data.ElasticSearch.Execution;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using static HotChocolate.Data.ElasticSearch.ElasticSearchContextData;

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
    {
        get;
    } = new(new ElasticSearchFilterCombinator());

    /// <inheritdoc />
    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new ElasticSearchQueryBuilder(CreateFilterDefinition(argumentName));

    private Func<IMiddlewareContext, BoolOperation?> CreateFilterDefinition(string argumentName)
        => context =>
        {
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            var skipFiltering = context.GetLocalStateOrDefault<bool>(SkipFilteringKey);
            context.SetLocalState(SkipFilteringKey, true);

            if (filter.IsNull() || skipFiltering || argument.Type is not IFilterInputType filterInput)
            {
                return null;
            }

            var visitorContext = new ElasticSearchFilterVisitorContext(filterInput);
            Visitor.Visit(filter, visitorContext);

            if (visitorContext.Errors.Count == 0)
            {
                return visitorContext.TryCreateQuery(out var whereQuery) ? whereQuery : null;
            }

            throw new GraphQLException(
                visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
        };

    private sealed class ElasticSearchQueryBuilder(
        Func<IMiddlewareContext, BoolOperation?> createFilterDefinition)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var filter = createFilterDefinition(context);
            context.SetLocalState(FilterDefinitionKey, filter);
        }

        public void Apply(IMiddlewareContext context)
        {
            if (context.Result is not IElasticSearchExecutable executable)
            {
                return;
            }

            var filter = context.GetLocalStateOrDefault<BoolOperation>(FilterDefinitionKey);
            if (filter is null)
            {
                return;
            }

            context.Result = executable.WithFiltering(filter);
        }
    }

    /// <inheritdoc />
    public override IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeDefinition,
        IFilterFieldConfiguration fieldDefinition)
    {
        if (fieldDefinition is not FilterFieldConfiguration definition)
        {
            return null;
        }

        var metadata = definition.Features.Get<ElasticFilterMetadata>();

        if (definition.Member?.GetCustomAttribute<ElasticSearchFieldNameAttribute>() is
            { Path: var path })
        {
            metadata ??= new ElasticFilterMetadata();
            metadata.Path = path;
        }

        return metadata;
    }
}
