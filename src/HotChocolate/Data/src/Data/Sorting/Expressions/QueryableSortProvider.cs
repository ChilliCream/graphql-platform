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
using HotChocolate.Utilities;

namespace HotChocolate.Data.Sorting.Expressions;

[return: NotNullIfNotNull("input")]
public delegate object? ApplySorting(IResolverContext context, object? input);

public class QueryableSortProvider : SortProvider<QueryableSortContext>
{
    public const string ContextArgumentNameKey = "SortArgumentName";
    public const string ContextVisitSortArgumentKey = nameof(VisitSortArgument);
    public const string SkipSortingKey = "SkipSorting";
    public const string ContextApplySortingKey = nameof(ApplySorting);

    public QueryableSortProvider()
    {
    }

    public QueryableSortProvider(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : base(configure)
    {
    }

    protected virtual SortVisitor<QueryableSortContext, QueryableSortOperation> Visitor { get; }
        = new();

    public override FieldMiddleware CreateExecutor<TEntityType>(string argumentName)
    {
        var applySorting = CreateApplicatorAsync<TEntityType>(argumentName.EnsureGraphQLName());

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

    protected virtual bool IsInMemoryQuery<TEntityType>(object? input)
    {
        if (input is QueryableExecutable<TEntityType> { InMemory: var inMemory })
        {
            return inMemory;
        }

        return input is not IQueryable || input is EnumerableQuery;
    }

    private ApplySorting CreateApplicatorAsync<TEntityType>(string argumentName)
    {
        return (context, input) =>
        {
            // next we get the sort argument.
            var argument = context.Selection.Field.Arguments[argumentName];
            var sort = context.ArgumentLiteral<IValueNode>(argumentName);

            // if no sort is defined we can stop here and yield back control.
            var skipSorting =
                context.LocalContextData.TryGetValue(SkipSortingKey, out var skip) &&
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
                context.Selection.Field.ContextData
                    .TryGetValue(ContextVisitSortArgumentKey, out var executorObj) &&
                executorObj is VisitSortArgument executor)
            {
                var inMemory = IsInMemoryQuery<TEntityType>(input);

                var visitorContext = executor(sort, sortInput, inMemory);

                // compile expression tree
                if (visitorContext.Errors.Count > 0)
                {
                    input = Array.Empty<TEntityType>();
                    foreach (var error in visitorContext.Errors)
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
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
        QueryableSortContext VisitSortArgumentExecutor(
            IValueNode valueNode,
            ISortInputType filterInput,
            bool inMemory)
        {
            var visitorContext = new QueryableSortContext(filterInput, inMemory);

            // rewrite GraphQL input object into expression tree.
            Visitor.Visit(valueNode, visitorContext);

            return visitorContext;
        }

        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitSortArgument)VisitSortArgumentExecutor;
        contextData[ContextVisitSortArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
    }

    public override ISortMetadata? CreateMetaData(
        ITypeCompletionContext context,
        ISortInputTypeDefinition typeDefinition,
        ISortFieldDefinition fieldDefinition)
    {
        if (fieldDefinition.Expression is not null)
        {
            if (fieldDefinition.Expression is not LambdaExpression lambda ||
                lambda.Parameters.Count != 1 ||
                lambda.Parameters[0].Type != typeDefinition.EntityType)
            {
                throw ThrowHelper.QueryableSortProvider_ExpressionParameterInvalid(
                    context.Type,
                    typeDefinition,
                    fieldDefinition);
            }

            return new ExpressionSortMetadata(fieldDefinition.Expression);
        }

        return null;
    }
}

public delegate QueryableSortContext VisitSortArgument(
    IValueNode filterValueNode,
    ISortInputType filterInputType,
    bool inMemory);
