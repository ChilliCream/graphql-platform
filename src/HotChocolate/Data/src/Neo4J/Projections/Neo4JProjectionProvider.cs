using System;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Projections.Extensions;
using HotChocolate.Data.Projections;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Neo4J.Projections
{
/// <inheritdoc/>
    public class Neo4JProjectionProvider
        : ProjectionProvider
    {
        /// <inheritdoc/>
        public Neo4JProjectionProvider()
        {
        }

        /// <inheritdoc/>
        public Neo4JProjectionProvider(
            Action<IProjectionProviderDescriptor> configure)
            : base(configure)
        {
        }

        /// <inheritdoc/>
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
                        new Neo4JProjectionVisitorContext(context, context.ObjectType);

                    var visitor = new ProjectionVisitor<Neo4JProjectionVisitorContext>();
                    visitor.Visit(visitorContext);

                    if (!visitorContext.TryCreateQuery(
                            out Neo4JProjectionDefinition? projections) ||
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
                        // context.LocalContextData =
                        //     context.LocalContextData.SetItem(
                        //         nameof(ProjectionDefinition<TEntityType>),
                        //         projections);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is INeo4JExecutable executable)
                        {
                            context.Result = executable.WithProjection(projections);
                        }
                    }
                }
            }
        }
    }
}
