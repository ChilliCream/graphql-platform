using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public delegate QueryableFilterContext VisitFilterArgument(
        IValueNode filterValueNode,
        IFilterInputType filterInputType,
        bool inMemory);

    [return: NotNullIfNotNull("input")]
    public delegate object? ApplyFiltering(IResolverContext context, object? input);

    public class QueryableFilterProvider
        : FilterProvider<QueryableFilterContext>
    {
        public static readonly string ContextArgumentNameKey = "FilterArgumentName";
        public static readonly string ContextVisitFilterArgumentKey = nameof(VisitFilterArgument);
        public static readonly string ContextApplyFilteringKey = nameof(ApplyFiltering);
        public static readonly string SkipFilteringKey = "SkipFiltering";
        public static readonly string ContextValueNodeKey = nameof(QueryableFilterProvider);

        public QueryableFilterProvider()
        {
        }

        public QueryableFilterProvider(
            Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
            : base(configure)
        {
        }

        protected virtual FilterVisitor<QueryableFilterContext, Expression> Visitor { get; } =
            new(new QueryableCombinator());

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            ApplyFiltering applyFilter = CreateApplicatorAsync<TEntityType>(argumentName);

            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(FieldDelegate next, IMiddlewareContext context)
            {
                context.LocalContextData =
                    context.LocalContextData.SetItem(ContextApplyFilteringKey, applyFilter);

                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

                context.Result = applyFilter(context, context.Result);
            }
        }

        private static ApplyFiltering CreateApplicatorAsync<TEntityType>(NameString argumentName)
        {
            return (context, input) =>
            {
                // next we get the filter argument. If the filter argument is already on the context
                // we use this. This enabled overriding the context with LocalContextData
                IInputField argument = context.Field.Arguments[argumentName];
                IValueNode filter = context.LocalContextData.ContainsKey(ContextValueNodeKey) &&
                    context.LocalContextData[ContextValueNodeKey] is IValueNode node
                        ? node
                        : context.ArgumentLiteral<IValueNode>(argumentName);

                // if no filter is defined we can stop here and yield back control.
                var skipFiltering =
                    context.LocalContextData.TryGetValue(SkipFilteringKey, out object? skip) &&
                    skip is true;

                // ensure filtering is only applied once
                context.LocalContextData =
                    context.LocalContextData.SetItem(SkipFilteringKey, true);

                if (filter.IsNull() || skipFiltering)
                {
                    return input;
                }

                if (argument.Type is IFilterInputType filterInput &&
                    context.Field.ContextData.TryGetValue(
                        ContextVisitFilterArgumentKey,
                        out object? executorObj) &&
                    executorObj is VisitFilterArgument executor)
                {
                    var inMemory =
                        input is QueryableExecutable<TEntityType> { InMemory: true } ||
                        input is not IQueryable ||
                        input is EnumerableQuery;

                    QueryableFilterContext visitorContext =
                        executor(filter, filterInput, inMemory);

                    // compile expression tree
                    if (visitorContext.TryCreateLambda(
                        out Expression<Func<TEntityType, bool>>? where))
                    {
                        input = input switch
                        {
                            IQueryable<TEntityType> q => q.Where(where),
                            IEnumerable<TEntityType> e => e.AsQueryable().Where(where),
                            QueryableExecutable<TEntityType> ex =>
                                ex.WithSource(ex.Source.Where(where)),
                            _ => input
                        };
                    }
                    else
                    {
                        if (visitorContext.Errors.Count > 0)
                        {
                            input = Array.Empty<TEntityType>();
                            foreach (IError error in visitorContext.Errors)
                            {
                                context.ReportError(error.WithPath(context.Path));
                            }
                        }
                    }
                }

                return input;
            };
        }

        public override void ConfigureField(
            NameString argumentName,
            IObjectFieldDescriptor descriptor)
        {
            QueryableFilterContext VisitFilterArgumentExecutor(
                IValueNode valueNode,
                IFilterInputType filterInput,
                bool inMemory)
            {
                var visitorContext =
                    new QueryableFilterContext(filterInput, inMemory);

                // rewrite GraphQL input object into expression tree.
                Visitor.Visit(valueNode, visitorContext);

                return visitorContext;
            }

            descriptor.ConfigureContextData(
                contextData =>
                {
                    contextData[ContextVisitFilterArgumentKey] =
                        (VisitFilterArgument)VisitFilterArgumentExecutor;
                    contextData[ContextArgumentNameKey] = argumentName;
                });
        }
    }
}
