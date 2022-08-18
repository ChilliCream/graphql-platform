using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Driver;

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
    protected virtual FilterVisitor<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
        Visitor
    { get; } = new(new MongoDbFilterCombinator());

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            MongoDbFilterVisitorContext? visitorContext = null;
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
            {
                visitorContext = new MongoDbFilterVisitorContext(filterInput);

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

                    context.LocalContextData = context.LocalContextData
                        .SetItem(nameof(FilterDefinition<TEntityType>), query);

                    await next(context).ConfigureAwait(false);

                    if (context.Result is IMongoDbExecutable executable)
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
