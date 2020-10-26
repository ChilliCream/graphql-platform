using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.MongoDb.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbFilterProvider
        : FilterProvider<MongoDbFilterVisitorContext>
    {
        public MongoDbFilterProvider()
        {
        }

        public MongoDbFilterProvider(
            Action<IFilterProviderDescriptor<MongoDbFilterVisitorContext>> configure)
            : base(configure)
        {
        }

        protected virtual FilterVisitor<MongoDbFilterVisitorContext, FilterDefinition<BsonDocument>>
            Visitor { get; } =
            new FilterVisitor<MongoDbFilterVisitorContext, FilterDefinition<BsonDocument>>(
                new MongoDbFilterCombinator());

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                MongoDbFilterVisitorContext? visitorContext = null;
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
                {
                    visitorContext = new MongoDbFilterVisitorContext(filterInput);

                    Visitor.Visit(filter, visitorContext);

                    if (!visitorContext.TryCreateQuery(out BsonDocument? whereQuery) ||
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
                                nameof(FilterDefinition<TEntityType>),
                                whereQuery);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is IAggregateFluentExecutable<TEntityType> aggregateFluentExecutable)
                        {
                            context.Result = aggregateFluentExecutable.Match(whereQuery);
                        }
                    }
                }
            }
        }
    }
}
