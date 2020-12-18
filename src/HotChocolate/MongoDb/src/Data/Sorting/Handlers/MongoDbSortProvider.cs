using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting.Convention.Extensions.Handlers
{
    /// <inheritdoc />
    public class MongoDbSortProvider
        : SortProvider<MongoDbSortVisitorContext>
    {
        public MongoDbSortProvider()
        {
        }

        public MongoDbSortProvider(
            Action<ISortProviderDescriptor<MongoDbSortVisitorContext>> configure)
            : base(configure)
        {
        }

        protected virtual SortVisitor<MongoDbSortVisitorContext, MongoDbSortDefinition>
            Visitor { get; } =
            new SortVisitor<MongoDbSortVisitorContext, MongoDbSortDefinition>();

        /// <inheritdoc />
        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                MongoDbSortVisitorContext? visitorContext = null;
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode &&
                    argument.Type is ListType listType &&
                    listType.ElementType is NonNullType nn &&
                    nn.NamedType() is SortInputType sortInputType)
                {
                    visitorContext = new MongoDbSortVisitorContext(sortInputType);

                    Visitor.Visit(filter, visitorContext);

                    if (!visitorContext.TryCreateQuery(out MongoDbSortDefinition? order) ||
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
}
