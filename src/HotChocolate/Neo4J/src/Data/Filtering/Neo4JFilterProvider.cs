#nullable enable
using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public class Neo4JFilterProvider
        : FilterProvider<Neo4JFilterVisitorContext>
    {
        /// <inheritdoc />
        public Neo4JFilterProvider()
        {
        }

        /// <inheritdoc />
        public Neo4JFilterProvider(
            Action<IFilterProviderDescriptor<Neo4JFilterVisitorContext>> configure)
            : base(configure)
        {
        }

        /// <summary>
        /// The visitor that is used to traverse the incoming selection set an execute handlers
        /// </summary>
        protected virtual FilterVisitor<Neo4JFilterVisitorContext, Condition>
            Visitor { get; } = new(new Neo4JFilterCombinator());

        /// <inheritdoc />
        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                Neo4JFilterVisitorContext? visitorContext = null;
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
                {
                    visitorContext = new Neo4JFilterVisitorContext(filterInput);

                    Visitor.Visit(filter, visitorContext);

                    if (!visitorContext.TryCreateQuery(out CompoundCondition whereQuery) ||
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
                                "Filter",
                                whereQuery);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is INeo4JExecutable executable)
                        {
                            context.Result = executable.WithFiltering(whereQuery);
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
