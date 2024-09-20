
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting;
using HotChocolate.Resolvers;
using static HotChocolate.Data.MongoDb.MongoDbContextData;

namespace HotChocolate.Data.MongoDb;

/// <inheritdoc/>
public class MongoDbProjectionProvider : ProjectionProvider
{
    /// <inheritdoc/>
    public MongoDbProjectionProvider()
    {
    }

    /// <inheritdoc/>
    public MongoDbProjectionProvider(
        Action<IProjectionProviderDescriptor> configure)
        : base(configure)
    {
    }

    public override IQueryBuilder CreateBuilder<TEntityType>()
        => new MongoDbQueryBuilder(CreateProjectionDefinition());

    private Func<IMiddlewareContext, MongoDbProjectionDefinition?> CreateProjectionDefinition()
        => context =>
        {
            // if no filter is defined we can stop here and yield back control.
            var skipProjection = context.GetLocalStateOrDefault<bool>(SkipProjectionKey);

            // ensure filtering is only applied once
            context.SetLocalState(SkipProjectionKey, true);

            if (skipProjection)
            {
                return null;
            }

            var visitorContext = new MongoDbProjectionVisitorContext(context, context.ObjectType);
            var visitor = new ProjectionVisitor<MongoDbProjectionVisitorContext>();
            visitor.Visit(visitorContext);

            if (visitorContext.Errors.Count == 0)
            {
                if(visitorContext.TryCreateQuery(out var projectionDef))
                {
                    return projectionDef;
                }

                return null;
            }

            throw new GraphQLException(
                visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
        };

    private sealed class MongoDbQueryBuilder(
        Func<IMiddlewareContext, MongoDbProjectionDefinition?> createProjectionDef)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var projectionDef = createProjectionDef(context);
            context.SetLocalState(ProjectionDefinitionKey, projectionDef);
        }

        public void Apply(IMiddlewareContext context)
        {
            if (context.Result is not IMongoDbExecutable executable)
            {
                return;
            }

            var filterDef = context.GetLocalStateOrDefault<MongoDbProjectionDefinition>(ProjectionDefinitionKey);

            if (filterDef is null)
            {
                return;
            }

            context.Result = executable.WithProjection(filterDef);
        }
    }
}
