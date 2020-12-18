using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a
    /// <see cref="FilterDefinition{T}"/>
    /// </summary>
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

        protected virtual FilterVisitor<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
            Visitor { get; } =
            new FilterVisitor<MongoDbFilterVisitorContext, MongoDbFilterDefinition>(
                new MongoDbFilterCombinator());

        /// <inheritdoc />
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

                    if (!visitorContext.TryCreateQuery(out MongoDbFilterDefinition? whereQuery) ||
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

                        if (context.Result is IMongoDbExecutable executable)
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
