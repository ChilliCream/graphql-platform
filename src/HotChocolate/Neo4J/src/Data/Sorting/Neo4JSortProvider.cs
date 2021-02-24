using System;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Sorting
{
    /// <inheritdoc />
    public class Neo4JSortProvider
        : SortProvider<Neo4JSortVisitorContext>
    {
        /// <inheritdoc/>
        public Neo4JSortProvider()
        {
        }

        /// <inheritdoc/>
        public Neo4JSortProvider(
            Action<ISortProviderDescriptor<Neo4JSortVisitorContext>> configure)
            : base(configure)
        {
        }

        /// <summary>
        /// The visitor thar will traverse a incoming query and execute the sorting handlers
        /// </summary>
        protected virtual SortVisitor<Neo4JSortVisitorContext, Neo4JSortDefinition>
            Visitor { get; } = new SortVisitor<Neo4JSortVisitorContext, Neo4JSortDefinition>();

        /// <inheritdoc />
        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode &&
                    argument.Type is ListType listType &&
                    listType.ElementType is NonNullType nn &&
                    nn.NamedType() is SortInputType sortInputType)
                {
                    var visitorContext = new Neo4JSortVisitorContext(sortInputType);

                    Visitor.Visit(filter, visitorContext);

                    if (!visitorContext.TryCreateQuery(out Neo4JSortDefinition order) ||
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
                                nameof(Neo4JSortDefinition),
                                order);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is INeo4JExecutable executable)
                        {
                            context.Result = executable.WithSorting(order);
                        }
                    }
                }
                else
                {
                    await next(context).ConfigureAwait(false);
                }
            }
        }
    }
}
