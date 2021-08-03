using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <summary>
    /// A <see cref="FilterProvider{TContext}"/> translates a incoming query to a
    /// <see cref="FilterDefinition{T}"/>
    /// </summary>
    public class SqlKataFilterProvider
        : FilterProvider<SqlKataFilterVisitorContext>
    {
        /// <inheritdoc />
        public SqlKataFilterProvider()
        {
        }

        /// <inheritdoc />
        public SqlKataFilterProvider(
            Action<IFilterProviderDescriptor<SqlKataFilterVisitorContext>> configure)
            : base(configure)
        {
        }

        /// <summary>
        /// The visitor that is used to traverse the incoming selection set an execute handlers
        /// </summary>
        protected virtual FilterVisitor<SqlKataFilterVisitorContext, Query>
            Visitor { get; } = new(new SqlKataFilterCombinator());

        /// <inheritdoc />
        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                SqlKataFilterVisitorContext? visitorContext = null;
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.ArgumentLiteral<IValueNode>(argumentName);

                if (filter is not NullValueNode && argument.Type is IFilterInputType filterInput)
                {
                    visitorContext = new SqlKataFilterVisitorContext(filterInput);

                    Visitor.Visit(filter, visitorContext);

                    if (!visitorContext.TryCreateQuery(out Query? whereQuery) ||
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
                                nameof(Query),
                                whereQuery);

                        await next(context).ConfigureAwait(false);

                        if (context.Result is ISqlKataExecutable executable)
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
