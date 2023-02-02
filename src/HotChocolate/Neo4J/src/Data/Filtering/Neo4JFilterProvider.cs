using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Neo4J.Filtering;

public class Neo4JFilterProvider : FilterProvider<Neo4JFilterVisitorContext>
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
    protected virtual FilterVisitor<Neo4JFilterVisitorContext, Condition> Visitor { get; } =
        new(new Neo4JFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            var argument = context.Selection.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                var visitorContext = new Neo4JFilterVisitorContext(filterInput);

                Visitor.Visit(filter, visitorContext);

                if (visitorContext.Errors.Count > 0)
                {
                    context.Result = Array.Empty<TEntityType>();
                    foreach (var error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }
                else
                {
                    var query = visitorContext.CreateQuery();

                    context.LocalContextData = context.LocalContextData.SetItem("Filter", query);

                    await next(context).ConfigureAwait(false);

                    if (context.Result is INeo4JExecutable executable)
                    {
                        context.Result = executable.WithFiltering(query);
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
