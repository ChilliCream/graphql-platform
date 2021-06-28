using System;
using System.Collections.Generic;
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

    public class QueryableFilterProvider
        : FilterProvider<QueryableFilterContext>
    {
        public static readonly string ContextArgumentNameKey = "FilterArgumentName";
        public static readonly string ContextVisitFilterArgumentKey = nameof(VisitFilterArgument);
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
            new FilterVisitor<QueryableFilterContext, Expression>(new QueryableCombinator());

        public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
        {
            return next => context => ExecuteAsync(next, context);

            async ValueTask ExecuteAsync(
                FieldDelegate next,
                IMiddlewareContext context)
            {
                // first we let the pipeline run and produce a result.
                await next(context).ConfigureAwait(false);

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

                if (filter.IsNull() || skipFiltering)
                {
                    return;
                }

                if (argument.Type is IFilterInputType filterInput &&
                    context.Field.ContextData.TryGetValue(
                        ContextVisitFilterArgumentKey,
                        out object? executorObj) &&
                    executorObj is VisitFilterArgument executor)
                {
                    var inMemory =
                        context.Result is QueryableExecutable<TEntityType> { InMemory: true } ||
                        context.Result is not IQueryable ||
                        context.Result is EnumerableQuery;

                    QueryableFilterContext visitorContext =
                        executor(filter, filterInput, inMemory);

                    // compile expression tree
                    if (visitorContext.TryCreateLambda(
                        out Expression<Func<TEntityType, bool>>? where))
                    {
                        context.Result = context.Result switch
                        {
                            IQueryable<TEntityType> q => q.Where(where),
                            IEnumerable<TEntityType> e => e.AsQueryable().Where(where),
                            QueryableExecutable<TEntityType> ex =>
                                ex.WithSource(ex.Source.Where(where)),
                            _ => context.Result
                        };
                    }
                    else
                    {
                        if (visitorContext.Errors.Count > 0)
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

        public override void ConfigureField(
            NameString argumentName,
            IObjectFieldDescriptor descriptor)
        {
            QueryableFilterContext VisitFilterArgumentExecutor(
                IValueNode valueNode,
                IFilterInputType filterInput,
                bool inMemory)
            {
                var visitorContext = new QueryableFilterContext(
                    filterInput,
                    inMemory);

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
