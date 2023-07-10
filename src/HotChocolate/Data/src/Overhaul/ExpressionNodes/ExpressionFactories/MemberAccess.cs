using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

[NoStructuralDependencies]
public sealed class MemberAccess : IExpressionFactory
{
    public MemberInfo Member { get; }

    public MemberAccess(MemberInfo member)
    {
        Member = member;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var instance = context.Expressions.Instance;
        return Expression.MakeMemberAccess(instance, Member);
    }
}
