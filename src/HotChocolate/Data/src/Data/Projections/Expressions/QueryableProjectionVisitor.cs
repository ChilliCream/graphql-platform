using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Data.Projections.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionVisitor : ProjectionVisitor<QueryableProjectionContext>
{
    protected override ISelectionVisitorAction VisitObjectType(
        IOutputField field,
        ObjectType objectType,
        ISelection selection,
        QueryableProjectionContext context)
    {
        var isAbstractType = field.Type.NamedType().IsAbstractType();
        if (isAbstractType && context.TryGetQueryableScope(out var scope))
        {
            var selections = context.ResolverContext.GetSelections(objectType, selection, true);

            if (selections.Count == 0)
            {
                return Continue;
            }

            context.PushInstance(
                Expression.Convert(context.GetInstance(), objectType.RuntimeType));
            scope.Level.Push(new Queue<Expression>());

            var res = base.VisitObjectType(field, objectType, selection, context);

            context.PopInstance();

            {
                var initializers = scope.Level.Pop();
                var initializersWithType = ProjectedValue.AppendObjectType(
                    initializers, objectType);

                scope.AddAbstractType(objectType.RuntimeType, initializersWithType);
            }

            return res;
        }

        return base.VisitObjectType(field, objectType, selection, context);
    }

    public static readonly QueryableProjectionVisitor Default = new();
}
