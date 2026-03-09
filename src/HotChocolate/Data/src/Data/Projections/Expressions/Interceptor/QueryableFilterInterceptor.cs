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

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableFilterInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(Selection selection) =>
        selection.Field.Member is PropertyInfo propertyInfo
        && propertyInfo.CanWrite
        && selection.HasFilterFeature();

    public void BeforeProjection(
        QueryableProjectionContext context,
        Selection selection)
    {
        var filterFeature = selection.GetFilterFeature();

        if (filterFeature is not null
            && context.Selections.Count > 0
            && context.Selections.Peek().Arguments.TryCoerceArguments(context.ResolverContext, out var coercedArgs)
            && coercedArgs.TryGetValue(filterFeature.ArgumentName, out var argumentValue)
            && argumentValue.Type is IFilterInputType filterInputType
            && argumentValue.ValueLiteral is { } valueNode and not NullValueNode)
        {
            var filterContext = filterFeature.ArgumentVisitor.Invoke(valueNode, filterInputType, false);
            var instance = context.PopInstance();

            if (filterContext.Errors.Count == 0)
            {
                if (filterContext.TryCreateLambda(out var expression))
                {
                    context.PushInstance(
                        Expression.Call(
                            typeof(Enumerable),
                            nameof(Enumerable.Where),
                            [filterInputType.EntityType.Source],
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

    public void AfterProjection(QueryableProjectionContext context, Selection selection)
    {
    }

    public static QueryableFilterInterceptor Create(ProjectionProviderContext context) => new();
}
