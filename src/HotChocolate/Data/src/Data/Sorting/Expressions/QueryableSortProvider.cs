using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting.Expressions
{
    [return: NotNullIfNotNull("input")]
    public delegate object? ApplySorting(IResolverContext context, object? input);

    public class QueryableSortProvider
        : SortProvider<QueryableSortContext>
    {
        public const string ContextArgumentNameKey = "SortArgumentName";
        public const string ContextVisitSortArgumentKey = nameof(VisitSortArgument);
        public const string SkipSortingKey = "SkipSorting";
        public const string ContextApplySortingKey = nameof(ApplySorting);

        public QueryableSortProvider()
        {
        }

        public QueryableSortProvider(
            Action<ISortProviderDescriptor<QueryableSortContext>> configure)
            : base(configure)
        {
        }

        protected virtual SortVisitor<QueryableSortContext, QueryableSortOperation> Visitor { get; }
            = new();

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            ApplySorting applySorting = CreateApplicatorAsync<TEntityType>(argumentName);

            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                context.LocalContextData =
                    context.LocalContextData.SetItem(ContextApplySortingKey, applySorting);

                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

                context.Result = applySorting(context, context.Result);
            }
        }

        private static ApplySorting CreateApplicatorAsync<TEntityType>(NameString argumentName)
        {
            return (context, input) =>
            {
                // next we get the sort argument.
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode sort = context.ArgumentLiteral<IValueNode>(argumentName);

                // if no sort is defined we can stop here and yield back control.
                var skipSorting =
                    context.LocalContextData.TryGetValue(SkipSortingKey, out object? skip) &&
                    skip is true;

                // ensure sorting is only applied once
                context.LocalContextData =
                    context.LocalContextData.SetItem(SkipSortingKey, true);

                if (sort.IsNull() || skipSorting)
                {
                    return input;
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
                        input is QueryableExecutable<TEntityType> { InMemory: true } ||
                        input is not IQueryable ||
                        input is EnumerableQuery;

                    QueryableSortContext visitorContext = executor(
                        sort,
                        sortInput,
                        inMemory);

                    // compile expression tree
                    if (visitorContext.Errors.Count > 0)
                    {
                        input = Array.Empty<TEntityType>();
                        foreach (IError error in visitorContext.Errors)
                        {
                            context.ReportError(error.WithPath(context.Path));
                        }
                    }
                    else
                    {
                        input = input switch
                        {
                            IQueryable<TEntityType> q => visitorContext.Sort(q),
                            IEnumerable<TEntityType> e => visitorContext.Sort(e.AsQueryable()),
                            QueryableExecutable<TEntityType> ex =>
                                ex.WithSource(visitorContext.Sort(ex.Source)),
                            _ => input
                        };
                    }
                }

                return input;
            };
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
