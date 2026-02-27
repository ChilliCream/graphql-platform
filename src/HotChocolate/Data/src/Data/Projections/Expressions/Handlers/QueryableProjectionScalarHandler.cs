using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

public class QueryableProjectionScalarHandler
    : QueryableProjectionHandlerBase
{
    public override bool CanHandle(Selection selection)
        => selection.IsLeaf
            && (selection.Field.Member is not null
                || selection.Field.ResolverExpression is LambdaExpression);

    public override bool TryHandleEnter(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        if (selection.Field.Member is PropertyInfo { CanWrite: true }
            || selection.Field.ResolverExpression is LambdaExpression)
        {
            action = SelectionVisitor.SkipAndLeave;
            return true;
        }

        action = SelectionVisitor.Skip;
        return true;
    }

    public override bool TryHandleLeave(
        QueryableProjectionContext context,
        Selection selection,
        [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        var field = selection.Field;

        if (context.Scopes.Count > 0
            && context.Scopes.Peek() is QueryableProjectionScope closure)
        {
            var instance = closure.Instance.Peek();

            if (field.Member is PropertyInfo member)
            {
                EnqueueBinding(
                    closure,
                    member,
                    Expression.Property(instance, member));

                action = SelectionVisitor.Continue;
                return true;
            }

            if (field.Member is null
                && field.ResolverExpression is LambdaExpression expression
                && expression.Parameters.Count == 1
                && expression.Parameters[0].Type.IsAssignableFrom(instance.Type))
            {
                var properties = TopLevelPropertyExtractor.Extract(expression);

                foreach (var property in properties)
                {
                    if (!property.CanWrite
                        || !(property.DeclaringType?.IsAssignableFrom(instance.Type) ?? false))
                    {
                        continue;
                    }

                    EnqueueBinding(
                        closure,
                        property,
                        Expression.Property(instance, property));
                }

                action = SelectionVisitor.Continue;
                return true;
            }
        }

        action = SelectionVisitor.Skip;
        return true;
    }

    private static void EnqueueBinding(
        QueryableProjectionScope scope,
        PropertyInfo member,
        Expression value)
    {
        if (scope.Level.Peek().Any(t => t.Member == member))
        {
            return;
        }

        scope.Level.Peek().Enqueue(Expression.Bind(member, value));
    }

    private sealed class TopLevelPropertyExtractor(ParameterExpression parameter) : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter = parameter;
        private readonly HashSet<PropertyInfo> _seen = [];
        private readonly List<PropertyInfo> _properties = [];

        public static IReadOnlyList<PropertyInfo> Extract(LambdaExpression expression)
        {
            var visitor = new TopLevelPropertyExtractor(expression.Parameters[0]);
            visitor.Visit(expression.Body);
            return visitor._properties;
        }

        protected override Expression VisitExtension(Expression node) => node.CanReduce ? base.VisitExtension(node) : node;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (TryGetTopLevelProperty(node, _parameter, out var property)
                && _seen.Add(property))
            {
                _properties.Add(property);
            }

            return base.VisitMember(node);
        }

        private static bool TryGetTopLevelProperty(
            Expression expression,
            ParameterExpression parameter,
            [NotNullWhen(true)] out PropertyInfo? property)
        {
            Expression? current = expression;

            while (current is not null)
            {
                current = UnwrapConvert(current);

                if (current is not MemberExpression memberExpression)
                {
                    break;
                }

                var parent = UnwrapConvert(memberExpression.Expression);

                if (parent == parameter)
                {
                    property = memberExpression.Member as PropertyInfo;
                    return property is not null;
                }

                if (parent is null)
                {
                    break;
                }

                current = parent;
            }

            property = null;
            return false;
        }

        private static Expression? UnwrapConvert(Expression? expression)
        {
            while (expression is UnaryExpression
                {
                    NodeType:
                        ExpressionType.Convert
                        or ExpressionType.ConvertChecked
                        or ExpressionType.TypeAs
                } unary)
            {
                expression = unary.Operand;
            }

            return expression;
        }
    }

    public static QueryableProjectionScalarHandler Create(ProjectionProviderContext context) => new();
}
