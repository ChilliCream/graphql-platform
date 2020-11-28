using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableSortInterceptor
        : IProjectionFieldInterceptor<QueryableProjectionContext>
    {
        public bool CanHandle(ISelection selection) =>
            selection.Field.Member is {} &&
            selection.Field.ContextData.ContainsKey(ContextVisitSortArgumentKey) &&
            selection.Field.ContextData.ContainsKey(ContextArgumentNameKey);

        public void BeforeProjection(
            QueryableProjectionContext context,
            ISelection selection)
        {
            IObjectField field = selection.Field;
            IReadOnlyDictionary<string, object?> contextData = field.ContextData;

            if (contextData.TryGetValue(ContextArgumentNameKey, out object? arg) &&
                arg is NameString argumentName &&
                contextData.TryGetValue(ContextVisitSortArgumentKey, out object? argVisitor) &&
                argVisitor is VisitSortArgument argumentVisitor &&
                context.Selection.Count > 0 &&
                context.Selection.Peek()
                    .Arguments.TryCoerceArguments(
                        context.Context.Variables,
                        context.Context.ReportError,
                        out IReadOnlyDictionary<NameString, ArgumentValue>? coercedArgs) &&
                coercedArgs.TryGetValue(argumentName, out var argumentValue) &&
                argumentValue.Argument.Type is ListType lt &&
                lt.ElementType is NonNullType nn &&
                nn.NamedType() is ISortInputType sortInputType &&
                argumentValue.ValueLiteral is {} valueNode &&
                valueNode is not NullValueNode)
            {
                QueryableSortContext sortContext =
                    argumentVisitor(valueNode, sortInputType, false);

                Expression instance = context.PopInstance();
                if (sortContext.Errors.Count == 0)
                {
                    context.PushInstance(sortContext.Compile(instance));
                }
                else
                {
                    context.PushInstance(
                        Expression.Constant(
                            Array.CreateInstance(sortInputType.EntityType.Source, 0)));
                    context.ReportError(
                        ProjectionProvider_CouldNotProjectSorting(valueNode));
                }
            }
        }

        public void AfterProjection(QueryableProjectionContext context, ISelection selection)
        {
        }
    }
}
