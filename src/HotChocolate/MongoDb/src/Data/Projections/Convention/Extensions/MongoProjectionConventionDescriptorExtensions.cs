using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Data.MongoDb;
using HotChocolate.Data.MongoDb.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    public abstract class MongoDbProjectionHandlerBase
        : ProjectionFieldHandler<MongoDbProjectionVisitorContext>
    {
        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            action = SelectionVisitor.Continue;
            return true;
        }
    }

    public class MongoDbProjectionFieldHandler
        : MongoDbProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is not null;

        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;
            context.Path.Push(field.GetName());
            action = SelectionVisitor.Continue;
            return true;
        }

        public override bool TryHandleLeave(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            [NotNullWhen(true)] out ISelectionVisitorAction? action)
        {
            context.Path.Pop();

            action = SelectionVisitor.Continue;
            return true;
        }
    }

    public class MongoDbProjectionScalarHandler
        : MongoDbProjectionHandlerBase
    {
        public override bool CanHandle(ISelection selection) =>
            selection.SelectionSet is null;

        public override bool TryHandleEnter(
            MongoDbProjectionVisitorContext context,
            ISelection selection,
            out ISelectionVisitorAction? action)
        {
            IObjectField field = selection.Field;
            context.Path.Push(field.GetName());
            context.Projections.Push(
                new MongoDbIncludeProjectionOperation(context.GetPath()));
            context.Path.Pop();

            action = SelectionVisitor.SkipAndLeave;
            return true;
        }
    }

    internal static class MongoProjectionVisitorContextExtensions
    {
        public static string GetPath(this MongoDbProjectionVisitorContext ctx) =>
            string.Join(".", ctx.Path.Reverse());

        public static bool TryCreateQuery(
            this MongoDbProjectionVisitorContext context,
            [NotNullWhen(true)] out MongoDbProjectionDefinition? query)
        {
            query = null;

            if (context.Projections.Count == 0)
            {
                return false;
            }

            query = new MongoDbCombinedProjectionDefinition(context.Projections.ToArray());
            return true;
        }
    }

    public static class MongoDbProjectionProviderDescriptorExtensions
    {
        public static IProjectionProviderDescriptor AddMongoDbDefaults(
            this IProjectionProviderDescriptor descriptor) =>
            descriptor.RegisteMongoDbHandlers();

        public static IProjectionProviderDescriptor RegisteMongoDbHandlers(
            this IProjectionProviderDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor.RegisterFieldHandler<MongoDbProjectionScalarHandler>();
            descriptor.RegisterFieldHandler<MongoDbProjectionFieldHandler>();
            descriptor.RegisterOptimizer<QueryablePagingProjectionOptimizer>();
            descriptor.RegisterOptimizer<IsProjectedProjectionOptimizer>();
            return descriptor;
        }
    }

    public static class MongoDbProjectionConventionDescriptorExtensions
    {
        public static IProjectionConventionDescriptor AddMongoDbDefaults(
            this IProjectionConventionDescriptor descriptor) =>
            descriptor.Provider(new MongoDbProjectionProvider(x => x.AddMongoDbDefaults()));
    }

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

    public class MongoDbProjectionVisitorContext
        : ProjectionVisitorContext<MongoDbProjectionDefinition>
    {
        public MongoDbProjectionVisitorContext(
            IResolverContext context,
            IOutputType initialType)
            : base(context, initialType, new MongoDbProjectionScope())
        {
        }

        public Stack<string> Path { get; } = new Stack<string>();

        public Stack<MongoDbProjectionDefinition> Projections { get; }
            = new Stack<MongoDbProjectionDefinition>();
    }

    public class MongoDbProjectionScope
        : ProjectionScope<MongoDbProjectionDefinition>
    {
    }
}
