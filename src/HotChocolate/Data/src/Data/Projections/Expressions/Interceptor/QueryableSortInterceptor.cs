using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Data.ErrorHelper;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableSortInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(ISelection selection) =>
        selection.Field.Member is PropertyInfo propertyInfo &&
        propertyInfo.CanWrite &&
        selection.Field.ContextData.ContainsKey(ContextVisitSortArgumentKey) &&
        selection.Field.ContextData.ContainsKey(ContextArgumentNameKey);

    public void BeforeProjection(
        QueryableProjectionContext context,
        ISelection selection)
    {
        var field = selection.Field;
        var contextData = field.ContextData;

        if (contextData.TryGetValue(ContextArgumentNameKey, out var arg) &&
            arg is string argumentName &&
            contextData.TryGetValue(ContextVisitSortArgumentKey, out var argVisitor) &&
            argVisitor is VisitSortArgument argumentVisitor &&
            context.Selection.Count > 0 &&
            context.Selection.Peek().Arguments
                .TryCoerceArguments(context.ResolverContext, out var coercedArgs) &&
            coercedArgs.TryGetValue(argumentName, out var argumentValue) &&
            argumentValue.Type is ListType lt &&
            lt.ElementType is NonNullType nn &&
            nn.NamedType() is ISortInputType sortInputType &&
            argumentValue.ValueLiteral is { } valueNode &&
            valueNode is not NullValueNode)
        {
            var sortContext =
                argumentVisitor(valueNode, sortInputType, false);

            var instance = context.PopInstance();
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
