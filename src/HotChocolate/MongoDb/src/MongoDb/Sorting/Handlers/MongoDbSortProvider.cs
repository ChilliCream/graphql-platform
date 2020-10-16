using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.MongoDb.Data.Sorting;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers
{
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

        protected virtual SortVisitor<MongoDbSortVisitorContext, SortDefinition<BsonDocument>>
            Visitor { get; } =
            new SortVisitor<MongoDbSortVisitorContext, SortDefinition<BsonDocument>>();

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

                if (filter is not NullValueNode && argument.Type is ISortInputType filterInput)
                {
                    visitorContext = new MongoDbSortVisitorContext(filterInput);

                    Visitor.Visit(filter, visitorContext);

                    if (visitorContext.TryCreateQuery(out BsonDocument? order))
                    {
                        context.LocalContextData =
                            context.LocalContextData.SetItem(
                                nameof(SortDefinition<TEntityType>),
                                order);
                    }

                    if (visitorContext.Errors.Count > 0)
                    {
                        context.Result = Array.Empty<TEntityType>();
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
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
}
