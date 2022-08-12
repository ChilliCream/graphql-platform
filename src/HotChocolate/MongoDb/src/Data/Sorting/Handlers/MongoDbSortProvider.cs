using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting;

/// <inheritdoc />
public class MongoDbSortProvider : SortProvider<MongoDbSortVisitorContext>
{
    /// <inheritdoc/>
    public MongoDbSortProvider()
    {
    }

    /// <inheritdoc/>
    public MongoDbSortProvider(
        Action<ISortProviderDescriptor<MongoDbSortVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor thar will traverse a incoming query and execute the sorting handlers
    /// </summary>
    protected virtual SortVisitor<MongoDbSortVisitorContext, MongoDbSortDefinition>
        Visitor
    { get; } = new();

    /// <inheritdoc />
    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            if (filter is not NullValueNode &&
                argument.Type is ListType listType &&
                listType.ElementType is NonNullType nn &&
                nn.NamedType() is SortInputType sortInputType)
            {
                var visitorContext = new MongoDbSortVisitorContext(sortInputType);

                Visitor.Visit(filter, visitorContext);

                if (!visitorContext.TryCreateQuery(out var order) ||
                    visitorContext.Errors.Count > 0)
                {
                    context.Result = Array.Empty<TEntityType>();
                    foreach (var error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }
                else
                {
                    context.LocalContextData =
                        context.LocalContextData.SetItem(
                            nameof(SortDefinition<TEntityType>),
                            order);

                    await next(context).ConfigureAwait(false);

                    if (context.Result is IMongoDbExecutable executable)
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
