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
                var skipSorting =
                    context.LocalContextData.TryGetValue(SkipSortingKey, out object? skip) &&
                    skip is true;

                if (sort.IsNull() || skipSorting)
                {
                    return;
                }

                if (argument.Type is ListType lt &&
                    lt.ElementType is NonNullType nn &&
                    nn.NamedType() is ISortInputType sortInput &&
                    context.Field.ContextData.TryGetValue(
                        ContextVisitSortArgumentKey,
                        out object? executorObj) &&
                    executorObj is VisitSortArgument executor)
                {
                    var inMemory =
                        context.Result is QueryableExecutable<TEntityType> { InMemory: true } ||
                        context.Result is not IQueryable ||
                        context.Result is EnumerableQuery;

                    QueryableSortContext visitorContext = executor(
                        sort,
                        sortInput,
                        inMemory);

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
                        context.Result = context.Result switch
                        {
                            IQueryable<TEntityType> q => visitorContext.Sort(q),
                            IEnumerable<TEntityType> e => visitorContext.Sort(e.AsQueryable()),
                            QueryableExecutable<TEntityType> ex =>
                                ex.WithSource(visitorContext.Sort(ex.Source)),
                            _ => context.Result
                        };
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
