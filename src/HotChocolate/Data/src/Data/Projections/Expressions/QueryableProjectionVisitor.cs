using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionVisitor
        : ProjectionVisitor<QueryableProjectionContext>
    {
        protected override ISelectionVisitorAction VisitObjectType(
            IOutputField field,
            ObjectType objectType,
            SelectionSetNode? selectionSet,
            QueryableProjectionContext context)
        {
            var isAbstractType = field.Type.NamedType().IsAbstractType();
            if (isAbstractType && context.TryGetQueryableScope(out QueryableProjectionScope? scope))
            {
                context.PushInstance(
                    Expression.Convert(context.GetInstance(), objectType.RuntimeType));
                scope.Level.Push(new Queue<MemberAssignment>());

                ISelectionVisitorAction res =
                    base.VisitObjectType(field, objectType, selectionSet, context);

                context.PopInstance();
                scope.AbstractType[objectType.RuntimeType] = scope.Level.Pop();

                return res;
            }

            return base.VisitObjectType(field, objectType, selectionSet, context);
        }
    }
}
