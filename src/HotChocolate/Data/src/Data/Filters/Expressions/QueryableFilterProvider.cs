using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public delegate QueryableFilterContext VisitFilterArgument(
    IValueNode filterValueNode,
    IFilterInputType filterInputType,
    bool inMemory);

[return: NotNullIfNotNull("input")]
public delegate object? ApplyFiltering(IResolverContext context, object? input);

public class QueryableFilterProvider : FilterProvider<QueryableFilterContext>
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

    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        var applyFilter = CreateApplicator<TEntityType>(argumentName);

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

    protected virtual bool IsInMemoryQuery<TEntityType>(object? input)
    {
        if (input is QueryableExecutable<TEntityType> { InMemory: var inMemory })
        {
            return inMemory;
        }

        return input is not IQueryable || input is EnumerableQuery;
    }

    private ApplyFiltering CreateApplicator<TEntityType>(string argumentName)
    {
        return (context, input) =>
        {
            // next we get the filter argument. If the filter argument is already on the context
            // we use this. This enabled overriding the context with LocalContextData
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.LocalContextData.ContainsKey(ContextValueNodeKey) &&
                context.LocalContextData[ContextValueNodeKey] is IValueNode node
                    ? node
                    : context.ArgumentLiteral<IValueNode>(argumentName);

            // if no filter is defined we can stop here and yield back control.
            var skipFiltering =
                context.LocalContextData.TryGetValue(SkipFilteringKey, out var skip) &&
                skip is true;

            // ensure filtering is only applied once
            context.LocalContextData =
                context.LocalContextData.SetItem(SkipFilteringKey, true);

            if (filter.IsNull() || skipFiltering)
            {
                return input;
            }

            if (argument.Type is IFilterInputType filterInput &&
                context.Selection.Field.ContextData
                    .TryGetValue(ContextVisitFilterArgumentKey, out var executorObj) &&
                executorObj is VisitFilterArgument executor)
            {
                var inMemory = IsInMemoryQuery<TEntityType>(input);

                var visitorContext = executor(filter, filterInput, inMemory);

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
                        foreach (var error in visitorContext.Errors)
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
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
        QueryableFilterContext VisitFilterArgumentExecutor(
            IValueNode valueNode,
            IFilterInputType filterInput,
            bool inMemory)
        {
            var visitorContext = new QueryableFilterContext(filterInput, inMemory);

            // rewrite GraphQL input object into expression tree.
            Visitor.Visit(valueNode, visitorContext);

            return visitorContext;
        }

        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitFilterArgument)VisitFilterArgumentExecutor;
        contextData[ContextVisitFilterArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
    }

    public override IFilterMetadata? CreateMetaData(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        if (fieldDefinition.Expression is not null)
        {
            if (fieldDefinition.Expression is not LambdaExpression lambda ||
                lambda.Parameters.Count != 1 ||
                lambda.Parameters[0].Type != typeDefinition.EntityType)
            {
                throw ThrowHelper.QueryableFilterProvider_ExpressionParameterInvalid(
                    context.Type,
                    typeDefinition,
                    fieldDefinition);
            }

            return new ExpressionFilterMetadata(fieldDefinition.Expression);
        }

        return null;
    }
}
