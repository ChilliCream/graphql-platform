using System;
using System.Threading.Tasks;
using HotChocolate.Data.MongoDb.Execution;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    public class MongoDbProjectionProvider
        : ProjectionProvider
    {
        public MongoDbProjectionProvider()
        {
        }

        public MongoDbProjectionProvider(
            Action<IProjectionProviderDescriptor> configure)
            : base(configure)
        {
        }

        public override FieldMiddleware CreateExecutor<TEntityType>()
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

                if (context.Result is not null)
                {
                    var visitorContext =
                        new MongoDbProjectionVisitorContext(context, context.ObjectType);

                    var visitor = new ProjectionVisitor<MongoDbProjectionVisitorContext>();
                    visitor.Visit(visitorContext);

                    if (!visitorContext.TryCreateQuery(
                            out MongoDbProjectionDefinition? projections) ||
                        visitorContext.Errors.Count > 0)
                    {
                        context.Result = Array.Empty<TEntityType>();
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                    else
                    {
                        context.LocalContextData =
                            context.LocalContextData.SetItem(
                                nameof(ProjectionDefinition<TEntityType>),
                                projections);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is IMongoExecutable executable)
                        {
                            context.Result = executable.WithProjection(projections);
                        }
                    }
                }
            }
        }
    }
}
