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

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data.Projections.Handlers;

public class QueryableSortInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(Selection selection)
        => selection.Field.Member is PropertyInfo propertyInfo
            && propertyInfo.CanWrite
            && selection.HasSortingFeature;

    public void BeforeProjection(
        QueryableProjectionContext context,
        Selection selection)
    {
        var field = selection.Field;

        if (field.Features.TryGet(out SortingFeature? feature)
            && context.Selections.Count > 0
            && context.Selections.Peek().Arguments.TryCoerceArguments(context.ResolverContext, out var coercedArgs)
            && coercedArgs.TryGetValue(feature.ArgumentName, out var argumentValue)
            && argumentValue.Type is ListType lt
            && lt.ElementType is NonNullType nn
            && nn.NamedType() is ISortInputType sortInputType
            && argumentValue.ValueLiteral is { } valueNode
            && valueNode is not NullValueNode)
        {
            var sortContext = feature.ArgumentVisitor(valueNode, sortInputType, false);

            var instance = context.PopInstance();
            if (sortContext.Errors.Count == 0)
            {
                context.PushInstance(sortContext.Compile(instance));
            }
            else
            {
                context.PushInstance(Expression.Constant(Array.CreateInstance(sortInputType.EntityType.Source, 0)));
                context.ReportError(ProjectionProvider_CouldNotProjectSorting(valueNode));
            }
        }
    }

    public void AfterProjection(QueryableProjectionContext context, Selection selection)
    {
    }

    public static QueryableSortInterceptor Create(ProjectionProviderContext context) => new();
}
