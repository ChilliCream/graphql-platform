using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

internal static class MemberAssignmentMerger
{
    public static bool TryMerge(
        MemberAssignment left,
        MemberAssignment right,
        out MemberAssignment merged)
    {
        if (left.Member == right.Member
            && TryMerge(left.Expression, right.Expression, out var value))
        {
            merged = left.Update(value);
            return true;
        }

        merged = left;
        return false;
    }

    private static bool TryMergeBindings(
        IReadOnlyList<MemberBinding> left,
        IReadOnlyList<MemberBinding> right,
        out List<MemberBinding> merged)
    {
        merged = new List<MemberBinding>(left.Count + right.Count);
        var indices = new Dictionary<MemberInfo, int>(left.Count + right.Count);

        return TryAddBindings(left, merged, indices)
            && TryAddBindings(right, merged, indices);
    }

    private static bool TryAddBindings(
        IReadOnlyList<MemberBinding> bindings,
        List<MemberBinding> merged,
        Dictionary<MemberInfo, int> indices)
    {
        foreach (var binding in bindings)
        {
            if (binding is not MemberAssignment assignment)
            {
                return false;
            }

            if (!indices.TryGetValue(assignment.Member, out var index))
            {
                indices.Add(assignment.Member, merged.Count);
                merged.Add(assignment);
                continue;
            }

            if (!TryMerge((MemberAssignment)merged[index], assignment, out var value))
            {
                return false;
            }

            merged[index] = value;
        }

        return true;
    }

    private static bool TryMerge(
        Expression left,
        Expression right,
        out Expression merged)
    {
        if (ReferenceEquals(left, right))
        {
            merged = left;
            return true;
        }

        switch (left, right)
        {
            case (MemberInitExpression l, MemberInitExpression r)
                when l.NewExpression.Type == r.NewExpression.Type
                    && l.NewExpression.Arguments.Count == 0
                    && r.NewExpression.Arguments.Count == 0
                    && TryMergeBindings(l.Bindings, r.Bindings, out var bindings):
                merged = l.Update(l.NewExpression, bindings);
                return true;

            case (ConditionalExpression l, ConditionalExpression r)
                when l.Type == r.Type
                    && AreEquivalent(l.Test, r.Test)
                    && TryMerge(l.IfTrue, r.IfTrue, out var ifTrue)
                    && TryMerge(l.IfFalse, r.IfFalse, out var ifFalse):
                merged = l.Update(l.Test, ifTrue, ifFalse);
                return true;

            case (UnaryExpression l, UnaryExpression r)
                when l.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs
                    && l.NodeType == r.NodeType
                    && l.Type == r.Type
                    && l.Method == r.Method
                    && TryMerge(l.Operand, r.Operand, out var operand):
                merged = l.Update(operand);
                return true;

            case (MethodCallExpression l, MethodCallExpression r)
                when l.Method == r.Method
                    && l.Method.DeclaringType == typeof(Enumerable)
                    && l.Method.Name == nameof(Enumerable.Select)
                    && AreEquivalent(l.Arguments[0], r.Arguments[0])
                    && TryMerge(l.Arguments[1], r.Arguments[1], out var selector):
                merged = l.Update(null, [l.Arguments[0], selector]);
                return true;

            case (MethodCallExpression l, MethodCallExpression r)
                when l.Method == r.Method
                    && l.Method.DeclaringType == typeof(Enumerable)
                    && l.Method.Name is nameof(Enumerable.ToList) or nameof(Enumerable.ToArray)
                    && TryMerge(l.Arguments[0], r.Arguments[0], out var source):
                merged = l.Update(null, [source]);
                return true;

            case (NewExpression l, NewExpression r)
                when l.Constructor == r.Constructor
                    && l.Arguments.Count == 1
                    && l.Type.IsGenericType
                    && (l.Type.GetGenericTypeDefinition() == typeof(HashSet<>)
                        || l.Type.GetGenericTypeDefinition() == typeof(SortedSet<>))
                    && TryMerge(l.Arguments[0], r.Arguments[0], out var source):
                merged = l.Update([source]);
                return true;

            case (LambdaExpression l, LambdaExpression r)
                when l.Type == r.Type
                    && l.Parameters.Count == 1
                    && r.Parameters.Count == 1
                    && TryMerge(
                        l.Body,
                        ParameterReplacer.Replace(r.Body, r.Parameters[0], l.Parameters[0]),
                        out var body):
                merged = Expression.Lambda(l.Type, body, l.Parameters);
                return true;
        }

        merged = left;
        return AreEquivalent(left, right);
    }

    private static bool AreEquivalent(
        IReadOnlyList<Expression> left,
        IReadOnlyList<Expression> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (!AreEquivalent(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool AreEquivalent(Expression? left, Expression? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.NodeType != right.NodeType || left.Type != right.Type)
        {
            return false;
        }

        return (left, right) switch
        {
            (MemberExpression l, MemberExpression r)
                => l.Member == r.Member && AreEquivalent(l.Expression, r.Expression),
            (ConstantExpression l, ConstantExpression r)
                => Equals(l.Value, r.Value),
            (UnaryExpression l, UnaryExpression r)
                => l.Method == r.Method && AreEquivalent(l.Operand, r.Operand),
            (BinaryExpression l, BinaryExpression r)
                => l.Method == r.Method
                    && l.IsLiftedToNull == r.IsLiftedToNull
                    && AreEquivalent(l.Left, r.Left)
                    && AreEquivalent(l.Right, r.Right)
                    && AreEquivalent(l.Conversion, r.Conversion),
            (TypeBinaryExpression l, TypeBinaryExpression r)
                => l.TypeOperand == r.TypeOperand && AreEquivalent(l.Expression, r.Expression),
            (DefaultExpression, DefaultExpression) => true,
            (MethodCallExpression l, MethodCallExpression r)
                => l.Method == r.Method
                    && AreEquivalent(l.Object, r.Object)
                    && AreEquivalent(l.Arguments, r.Arguments),
            (LambdaExpression l, LambdaExpression r)
                => l.Parameters.Count == 1
                    && r.Parameters.Count == 1
                    && AreEquivalent(
                        l.Body,
                        ParameterReplacer.Replace(r.Body, r.Parameters[0], l.Parameters[0])),
            (NewArrayExpression l, NewArrayExpression r)
                => AreEquivalent(l.Expressions, r.Expressions),
            (ConditionalExpression l, ConditionalExpression r)
                => AreEquivalent(l.Test, r.Test)
                    && AreEquivalent(l.IfTrue, r.IfTrue)
                    && AreEquivalent(l.IfFalse, r.IfFalse),
            _ => false
        };
    }

    private sealed class ParameterReplacer(
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
        : ExpressionVisitor
    {
        public static Expression Replace(
            Expression expression,
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
            => new ParameterReplacer(oldParameter, newParameter).Visit(expression);

        protected override Expression VisitParameter(ParameterExpression node)
            => node == oldParameter ? newParameter : node;
    }
}
