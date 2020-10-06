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
        public const string ContextArgumentNameKey = "SortArgumentName";
        public const string ContextVisitSortArgumentKey = nameof(VisitSortArgument);
        public const string SkipSortingKey = "SkipSorting";

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
                if (sort.IsNull() ||
                    (context.LocalContextData.TryGetValue(
                            SkipSortingKey,
                            out object? skipObject) &&
                        skipObject is bool skip &&
                        skip))
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

                if (source != null &&
                    argument.Type is ISortInputType sortInput &&
                    context.Field.ContextData.TryGetValue(
                        ContextVisitSortArgumentKey,
                        out object? executorObj) &&
                    executorObj is VisitSortArgument executor)
                {
                    QueryableSortContext visitorContext = executor(
                        sort,
                        sortInput,
                        source is EnumerableQuery);

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

        public override void ConfigureField(
            NameString argumentName,
            IObjectFieldDescriptor descriptor)
        {
            QueryableSortContext VisitSortArgumentExecutor(
                IValueNode valueNode,
                ISortInputType filterInput,
                bool inMemory)
            {
                var visitorContext = new QueryableSortContext(
                    filterInput,
                    inMemory);

                // rewrite GraphQL input object into expression tree.
                Visitor.Visit(valueNode, visitorContext);

                return visitorContext;
            }

            descriptor.ConfigureContextData(
                contextData =>
                {
                    contextData[ContextVisitSortArgumentKey] =
                        (VisitSortArgument)VisitSortArgumentExecutor;
                    contextData[ContextArgumentNameKey] = argumentName;
                });
        }
    }

    public delegate QueryableSortContext VisitSortArgument(
        IValueNode filterValueNode,
        ISortInputType filterInputType,
        bool inMemory);
}
