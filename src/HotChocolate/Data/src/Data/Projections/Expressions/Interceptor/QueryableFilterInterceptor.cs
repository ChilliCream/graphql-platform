using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableFilterInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(ISelection selection) =>
        selection.Field.Member is PropertyInfo propertyInfo &&
        propertyInfo.CanWrite &&
        selection.Field.ContextData.ContainsKey(ContextVisitFilterArgumentKey) &&
        selection.Field.ContextData.ContainsKey(ContextArgumentNameKey);

    public void BeforeProjection(
        QueryableProjectionContext context,
        ISelection selection)
    {
        var field = selection.Field;
        var contextData = field.ContextData;

        if (contextData.TryGetValue(ContextArgumentNameKey, out var arg) &&
            arg is string argumentName &&
            contextData.TryGetValue(ContextVisitFilterArgumentKey, out var argVisitor) &&
            argVisitor is VisitFilterArgument argumentVisitor &&
            context.Selection.Count > 0 &&
            context.Selection.Peek().Arguments
                .TryCoerceArguments(context.ResolverContext, out var coercedArgs) &&
            coercedArgs.TryGetValue(argumentName, out var argumentValue) &&
            argumentValue.Type is IFilterInputType filterInputType &&
            argumentValue.ValueLiteral is { } valueNode and not NullValueNode)
        {
            var filterContext =
                argumentVisitor(valueNode, filterInputType, false);

            var instance = context.PopInstance();
            if (filterContext.Errors.Count == 0)
            {
                if (filterContext.TryCreateLambda(out var expression))
                {
                    context.PushInstance(
                        Expression.Call(
                            typeof(Enumerable),
                            nameof(Enumerable.Where),
                            [filterInputType.EntityType.Source,],
                            instance,
                            expression));
                }
                else
                {
                    context.PushInstance(instance);
                }
            }
            else
            {
                context.PushInstance(
                    Expression.Constant(
                        Array.CreateInstance(filterInputType.EntityType.Source, 0)));
                context.ReportError(
                    ProjectionProvider_CouldNotProjectFiltering(valueNode));
            }
        }
    }

    public void AfterProjection(QueryableProjectionContext context, ISelection selection)
    {
    }
}
