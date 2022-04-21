using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Types;

namespace HotChocolate.Data.Extensions;

[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Late bound")]
internal class EntityFrameworkQueryableProjectionObjectHandler : QueryableProjectionObjectHandler<EntityFrameworkQueryableProjectionContext>
{
    protected override MemberAssignment ConstructMemberAssignment(EntityFrameworkQueryableProjectionContext context, IObjectField field, Expression nestedProperty, Expression memberInit)
    {
        MemberInfo[]? keyMembers = null;
        if (context.UseKeysForNullCheck && field.Type is IObjectType objectType)
            keyMembers = objectType.KeyMembers;

        var bindingExpression = EntityFrameworkProjectionExpressionBuilder.NotNullAndAlso(nestedProperty, keyMembers, memberInit);

        return Expression.Bind(field.Member!, bindingExpression);
    }
}
