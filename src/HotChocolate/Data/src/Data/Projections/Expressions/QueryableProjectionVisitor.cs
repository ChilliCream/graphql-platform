using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionVisitor : ProjectionVisitor<QueryableProjectionContext>
{
    protected override ISelectionVisitorAction VisitObjectType(
        IOutputFieldDefinition field,
        ObjectType objectType,
        Selection selection,
        QueryableProjectionContext context)
    {
        var isAbstractType = field.Type.NamedType().IsAbstractType();

        if (!isAbstractType || !context.TryGetQueryableScope(out var scope))
        {
            return base.VisitObjectType(field, objectType, selection, context);
        }

        var selections = context.GetSelections(objectType, selection, true);

        if (!selections.Any())
        {
            return Continue;
        }

        context.PushInstance(Expression.Convert(context.GetInstance(), objectType.RuntimeType));
        scope.Level.Push(new Queue<MemberAssignment>());

        var res = base.VisitObjectType(field, objectType, selection, context);

        context.PopInstance();
        scope.AddAbstractType(objectType.RuntimeType, scope.Level.Pop());

        return res;
    }

    public static readonly QueryableProjectionVisitor Default = new();
}
