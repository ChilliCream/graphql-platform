using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

internal sealed class ProjectionDependencyCollector(
    QueryableProjectionScope scope,
    NullabilityInfoContext nullabilityInfoContext,
    ParameterExpression parameter) : ExpressionVisitor
{
    public static void Collect(
        QueryableProjectionScope scope,
        NullabilityInfoContext nullabilityInfoContext,
        Expression expression,
        ParameterExpression parameter)
        => new ProjectionDependencyCollector(scope, nullabilityInfoContext, parameter).Visit(expression);

    protected override Expression VisitMember(MemberExpression node)
    {
        var path = new List<PropertyInfo>();
        Expression? current = node;

        while (current is MemberExpression { Member: PropertyInfo property } member)
        {
            path.Add(property);
            current = member.Expression;
        }

        if (current == parameter && path[^1].CanWrite)
        {
            scope.Level.Peek().Enqueue(CreateBinding(scope.Parameter, path, path.Count - 1));
            return node;
        }

        return base.VisitMember(node);
    }

    private MemberAssignment CreateBinding(
        Expression instance,
        List<PropertyInfo> path,
        int index)
    {
        var property = path[index];
        var access = Expression.Property(instance, property);

        if (index == 0 || !path[index - 1].CanWrite)
        {
            return Expression.Bind(property, access);
        }

        var constructor = property.PropertyType.GetConstructor(Type.EmptyTypes);

        if (constructor is null)
        {
            return Expression.Bind(property, access);
        }

        Expression value = Expression.MemberInit(
            Expression.New(constructor),
            CreateBinding(access, path, index - 1));

        if (!property.PropertyType.IsValueType
            && nullabilityInfoContext.Create(property).ReadState != NullabilityState.NotNull)
        {
            value = Expression.Condition(
                Expression.Equal(access, Expression.Constant(null, access.Type)),
                Expression.Default(access.Type),
                value);
        }

        return Expression.Bind(property, value);
    }
}
