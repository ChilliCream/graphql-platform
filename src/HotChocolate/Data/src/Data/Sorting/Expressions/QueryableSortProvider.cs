using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class QueryableSortProvider
        : SortProvider<QueryableSortContext>
    {
        public QueryableSortProvider()
        {
        }

        public QueryableSortProvider(
            Action<ISortProviderDescriptor<QueryableSortContext>> configure)
            : base(configure)
        {
        }

        protected virtual SortVisitor<QueryableSortContext, QueryableSortOperation> Visitor { get; }
            = new SortVisitor<QueryableSortContext, QueryableSortOperation>();

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

                // next we get the sort argument.
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode sort = context.ArgumentLiteral<IValueNode>(argumentName);

                // if no sort is defined we can stop here and yield back control.
                if (sort.IsNull())
                {
                    return;
                }

                IQueryable<TEntityType>? source = null;

                if (context.Result is IQueryable<TEntityType> q)
                {
                    source = q;
                }
                else if (context.Result is IEnumerable<TEntityType> e)
                {
                    source = e.AsQueryable();
                }

                if (source != null && argument.Type is ListType lt &&
                    lt.ElementType is ISortInputType sortInput)
                {
                    var visitorContext = new QueryableSortContext(
                        sortInput,
                        source is EnumerableQuery);

                    // rewrite GraphQL input object into expression tree.
                    Visitor.Visit(sort, visitorContext);

                    // compile expression tree
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
                        context.Result = visitorContext.Sort(source);
                    }
                }
            }
        }
    }
}
