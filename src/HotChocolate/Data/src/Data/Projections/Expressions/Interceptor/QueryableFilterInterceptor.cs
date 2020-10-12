using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableFilterInterceptor
        : IProjectionFieldInterceptor<QueryableProjectionContext>
    {
        public bool CanHandle(ISelection field) =>
            field.Field.Member is {} &&
            field.Field.ContextData.ContainsKey(ContextVisitFilterArgumentKey) &&
            field.Field.ContextData.ContainsKey(ContextArgumentNameKey);

        public void BeforeProjection(
            QueryableProjectionContext context,
            ISelection selection)
        {
            IObjectField field = selection.Field;
            IReadOnlyDictionary<string, object?> contextData = field.ContextData;

            if (contextData.TryGetValue(ContextArgumentNameKey, out object? arg) &&
                arg is NameString argumentName &&
                contextData.TryGetValue(ContextVisitFilterArgumentKey, out object? argVisitor) &&
                argVisitor is VisitFilterArgument argumentVisitor &&
                context.Selection.Count > 0 &&
                context.Selection.Peek()
                    .Arguments.TryCoerceArguments(
                        context.Context.Variables,
                        context.Context.ReportError,
                        out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs) &&
                coercedArgs.TryGetValue(argumentName, out var argumentValue) &&
                argumentValue.Argument.Type is IFilterInputType filterInputType &&
                argumentValue.ValueLiteral is {} valueNode)
            {
                QueryableFilterContext filterContext =
                    argumentVisitor(valueNode, filterInputType, false);

                Expression instance = context.PopInstance();
                if (filterContext.Errors.Count == 0 &&
                    filterContext.TryCreateLambda(out LambdaExpression? expression))
                {
                    context.PushInstance(
                        Expression.Call(
                            typeof(Enumerable),
                            nameof(Enumerable.Where),
                            new[] { filterInputType.EntityType.Source },
                            instance,
                            (Expression)expression));
                }
                else
                {
                    context.PushInstance(
                        Expression.Constant(Array.CreateInstance(filterInputType.RuntimeType, 0)));
                    // TODO: Report error and replace with empty array
                }
            }
        }

        public void AfterProjection(QueryableProjectionContext context, ISelection selection)
        {
        }
    }
}
