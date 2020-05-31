using System;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public class FilterMongoVisitorMiddleware
        : IFilterMiddleware<FilterDefinition<BsonDocument>>
    {
        public async Task ApplyFilter<T>(
            FilterVisitorDefinition<FilterDefinition<BsonDocument>> definition,
            IMiddlewareContext context,
            FieldDelegate next,
            IFilterConvention filterConvention,
            ITypeConversion converter,
            IFilterInputType fit,
            InputObjectType iot)
        {
            MongoFilterVisitorContext? visitorContext = null;
            string argumentName = filterConvention!.GetArgumentName();
            IValueNode filter = context.Argument<IValueNode>(argumentName);

            if (!(filter is null || filter is NullValueNode))
            {
                visitorContext = new MongoFilterVisitorContext(fit, definition, converter);

                FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(filter, visitorContext);

                if (visitorContext.TryCreateQuery(out BsonDocument? whereQuery))
                {
                    context.LocalContextData =
                        context.LocalContextData.SetItem(
                            nameof(FilterDefinition<T>), whereQuery);
                }
            }

            await next(context).ConfigureAwait(false);

            if (visitorContext is { } && visitorContext.Errors.Count > 0)
            {
                context.Result = Array.Empty<T>();
                foreach (IError error in visitorContext.Errors)
                {
                    context.ReportError(error.WithPath(context.Path));
                }
            }
        }

    }
}
