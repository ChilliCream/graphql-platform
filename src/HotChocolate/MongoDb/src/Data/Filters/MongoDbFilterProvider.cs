using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using MongoDB.Driver;
using static HotChocolate.Data.MongoDb.MongoDbContextData;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a
/// <see cref="FilterDefinition{T}"/>
/// </summary>
public class MongoDbFilterProvider : FilterProvider<MongoDbFilterVisitorContext>
{
    /// <inheritdoc />
    public MongoDbFilterProvider()
    {
    }

    /// <inheritdoc />
    public MongoDbFilterProvider(
        Action<IFilterProviderDescriptor<MongoDbFilterVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to traverse the incoming selection set an execute handlers
    /// </summary>
    protected virtual FilterVisitor<MongoDbFilterVisitorContext, MongoDbFilterDefinition> Visitor { get; } =
        new(new MongoDbFilterCombinator());

    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new MongoDbQueryBuilder(CreateFilterDefinition(argumentName));

    private Func<IMiddlewareContext, MongoDbFilterDefinition?> CreateFilterDefinition(string argumentName)
        => context =>
        {
            // next we get the filter argument.
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            // if no filter is defined we can stop here and yield back control.
            var skipFiltering = context.GetLocalStateOrDefault<bool>(SkipFilteringKey);

            // ensure filtering is only applied once
            context.SetLocalState(SkipFilteringKey, true);

            if (filter.IsNull() || skipFiltering || argument.Type is not IFilterInputType filterInput)
            {
                return null;
            }

            var visitorContext = new MongoDbFilterVisitorContext(filterInput);

            Visitor.Visit(filter, visitorContext);

            if (visitorContext.Errors.Count == 0)
            {
                return visitorContext.CreateQuery();
            }

            throw new GraphQLException(
                visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
        };

    private sealed class MongoDbQueryBuilder(
        Func<IMiddlewareContext, MongoDbFilterDefinition?> createFilterDef)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var filterDef = createFilterDef(context);
            context.SetLocalState(FilterDefinitionKey, filterDef);
        }

        public void Apply(IMiddlewareContext context)
        {
            if (context.Result is not IMongoDbExecutable executable)
            {
                return;
            }

            var filterDef = context.GetLocalStateOrDefault<MongoDbFilterDefinition>(FilterDefinitionKey);

            if (filterDef is null)
            {
                return;
            }

            context.Result = executable.WithFiltering(filterDef);
        }
    }
}
