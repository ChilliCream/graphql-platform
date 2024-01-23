using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Data.Filters.Expressions;

/// <summary>
/// The default handler for all <see cref="FilterField"/> for the
/// <see cref="QueryableFilterProvider"/>
/// </summary>
public class QueryableDefaultFieldHandler
    : FilterFieldHandler<QueryableFilterContext, Expression>
{
    /// <summary>
    /// Checks if the field not a filter operations field and if the member is defined on this
    /// field
    /// </summary>
    /// <param name="context">The current context</param>
    /// <param name="typeDefinition">The definition of the type that declares the field</param>
    /// <param name="fieldDefinition">The definition of the field</param>
    /// <returns>True in case the field can be handled</returns>
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition) =>
        fieldDefinition is not FilterOperationFieldDefinition &&
        (fieldDefinition.Member is not null || fieldDefinition.Expression is not null);

    public override bool TryHandleEnter(
        QueryableFilterContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (node.Value.IsNull())
        {
            context.ReportError(
                ErrorHelper.CreateNonNullError(field, node.Value, context));

            action = SyntaxVisitor.Skip;

            return true;
        }

        if (field.RuntimeType is null)
        {
            action = null;

            return false;
        }

        Expression nestedProperty;
        if (field.Metadata is ExpressionFilterMetadata { Expression: LambdaExpression expression, })
        {
            if (expression.Parameters.Count != 1 ||
                expression.Parameters[0].Type != context.RuntimeTypes.Peek()!.Source)
            {
                throw ThrowHelper.QueryableFiltering_ExpressionParameterInvalid(
                    field.RuntimeType.Source,
                    field);
            }

            nestedProperty = ReplaceVariableExpressionVisitor
                .ReplaceParameter(expression, expression.Parameters[0], context.GetInstance())
                .Body;
        }
        else
        {
            var instance = context.GetInstance();

            // we need to check if the previous value was a nullable value type. if it is a nullable
            // value type we cannot just chain the next expression to it. We have to first select
            // ".Value".
            //
            // without this check we would chain "previous" directly to "current": previous.current
            // with this check we chain "previous" via ".Value" to "current": previous.Value.current
            if (context.TryGetPreviousRuntimeType(out var previousRuntimeType) &&
                previousRuntimeType.IsNullableValueType())
            {
                var valueGetter = instance.Type.GetProperty(nameof(Nullable<int>.Value));
                instance = Expression.Property(instance, valueGetter!);
            }

            nestedProperty = field.Member switch
            {
                PropertyInfo propertyInfo => Expression.Property(instance, propertyInfo),

                MethodInfo methodInfo => Expression.Call(instance, methodInfo),

                null => throw ThrowHelper.QueryableFiltering_NoMemberDeclared(field),

                _ => throw ThrowHelper.QueryableFiltering_MemberInvalid(field.Member, field)
            };
        }

        context.PushInstance(nestedProperty);
        context.RuntimeTypes.Push(field.RuntimeType);
        action = SyntaxVisitor.Continue;

        return true;
    }

    public override bool TryHandleLeave(
        QueryableFilterContext context,
        IFilterField field,
        ObjectFieldNode node,
        [NotNullWhen(true)] out ISyntaxVisitorAction? action)
    {
        if (field.RuntimeType is null)
        {
            action = null;

            return false;
        }

        // Deque last
        var condition = context.GetLevel().Dequeue();

        context.PopInstance();
        context.RuntimeTypes.Pop();

        // when we are in a in-memory context, it is possible that we have null reference exceptions
        // To avoid these exceptions, we need to add null checks to the chain. We always wrap the
        // field before in a null check.
        //
        // reference types:
        //    previous.current > 10   ==>    previous is not null && previous.current > 10
        //
        // structs:
        //    previous.Value.current > 10   ==> previous is not null && previous.Value.current > 10
        //
        if (context.InMemory &&
            context.TryGetPreviousRuntimeType(out var previousRuntimeType) &&
            (previousRuntimeType.IsNullableValueType() || !previousRuntimeType.IsValueType()))
        {
            var peekedInstance = context.GetInstance();
            condition = FilterExpressionBuilder.NotNullAndAlso(peekedInstance, condition);
        }

        context.GetLevel().Enqueue(condition);
        action = SyntaxVisitor.Continue;

        return true;
    }

    private sealed class ReplaceVariableExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _replacement;
        private readonly ParameterExpression _parameter;

        public ReplaceVariableExpressionVisitor(
            Expression replacement,
            ParameterExpression parameter)
        {
            _replacement = replacement;
            _parameter = parameter;
        }

        protected override Expression VisitExtension(Expression node) => node.CanReduce ? base.VisitExtension(node) : node;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _parameter)
            {
                return _replacement;
            }

            return base.VisitParameter(node);
        }

        public static LambdaExpression ReplaceParameter(
            LambdaExpression lambda,
            ParameterExpression parameter,
            Expression replacement)
            => (LambdaExpression)
                new ReplaceVariableExpressionVisitor(replacement, parameter).Visit(lambda);
    }
}

static file class LocalExtensions
{
    public static bool TryGetPreviousRuntimeType(
        this QueryableFilterContext context,
        [NotNullWhen(true)] out IExtendedType? runtimeType)
    {
        return context.RuntimeTypes.TryPeek(out runtimeType);
    }

    public static bool IsNullableValueType(this IExtendedType type)
    {
        return type.GetTypeOrElementType() is { Type.IsValueType: true, IsNullable: true, };
    }

    public static bool IsValueType(this IExtendedType type)
    {
        return type.GetTypeOrElementType() is { Type.IsValueType: true, };
    }

    private static IExtendedType GetTypeOrElementType(this IExtendedType type)
    {
        while (type is { IsArrayOrList: true, ElementType: { } nextType, })
        {
            type = nextType;
        }

        return type;
    }
}
