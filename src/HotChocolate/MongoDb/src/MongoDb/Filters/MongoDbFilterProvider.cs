using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoFilterProvider
        : FilterProvider<MongoFilterVisitorContext>
    {
        public MongoFilterProvider()
        {
        }

        public MongoFilterProvider(
            Action<IFilterProviderDescriptor<MongoFilterVisitorContext>> configure)
            : base(configure)
        {
        }

        protected virtual FilterVisitor<MongoFilterVisitorContext, FilterDefinition<BsonDocument>>
            Visitor { get; } =
            new FilterVisitor<MongoFilterVisitorContext, FilterDefinition<BsonDocument>>(
                new FilterMongoCombinator());

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                MongoFilterVisitorContext? visitorContext = null;
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
                {
                    visitorContext = new MongoFilterVisitorContext(filterInput);

                    Visitor.Visit(filter, visitorContext);

                    if (visitorContext.TryCreateQuery(out BsonDocument? whereQuery))
                    {
                        context.LocalContextData =
                            context.LocalContextData.SetItem(
                                nameof(FilterDefinition<TEntityType>),
                                whereQuery);
                    }
                }

                await next(context).ConfigureAwait(false);

                if (visitorContext is { } && visitorContext.Errors.Count > 0)
                {
                    context.Result = Array.Empty<TEntityType>();
                    foreach (IError error in visitorContext.Errors)
                    {
                        context.ReportError(error.WithPath(context.Path));
                    }
                }
            }
        }
    }
}
