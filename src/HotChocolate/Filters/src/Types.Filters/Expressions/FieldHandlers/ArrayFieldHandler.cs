using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class ArrayFieldHandler
    {
        public static bool Enter(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            if (context is QueryableFilterVisitorContext ctx)
            {
                if (FilterOperationKind.ArraySome.Equals(field.Operation.Kind)
                    || FilterOperationKind.ArrayNone.Equals(field.Operation.Kind)
                    || FilterOperationKind.ArrayAll.Equals(field.Operation.Kind))
                {
                    if (!field.Operation.IsNullable && node.Value.IsNull())
                    {
                        context.ReportError(
                            ErrorHelper.CreateNonNullError(field, node, context));

                        action = SyntaxVisitor.Skip;
                        return true;
                    }

                    MemberExpression nestedProperty = Expression.Property(
                        context.GetInstance(),
                        field.Operation.Property);

                    context.PushInstance(nestedProperty);

                    Type closureType = GetTypeFor(field.Operation);

                    ctx.ClrTypes.Push(closureType);

                    if (node.Value.IsNull())
                    {
                        ctx.AddIsNullClosure();

                        action = SyntaxVisitor.SkipAndLeave;
                    }
                    else
                    {
                        context.AddScope();
                        action = SyntaxVisitor.Continue;
                    }
                    return true;
                }
                action = null;
                return false;
            }

            throw new InvalidOperationException();
        }

        public static void Leave(
            FilterOperationField field,
            ObjectFieldNode node,
            IFilterVisitorContext<Expression> context)
        {
            if (context is QueryableFilterVisitorContext ctx)
            {
                if (FilterOperationKind.ArraySome.Equals(field.Operation.Kind)
                    || FilterOperationKind.ArrayNone.Equals(field.Operation.Kind)
                    || FilterOperationKind.ArrayAll.Equals(field.Operation.Kind))
                {
                    FilterScope<Expression> nestedScope = ctx.PopScope();

                    ctx.ClrTypes.Pop();

                    if (nestedScope is QueryableScope nestedClosure &&
                        nestedClosure.TryCreateLambda(out LambdaExpression? lambda))
                    {
                        Type closureType = GetTypeFor(field.Operation);

                        Expression expression;
                        switch (field.Operation.Kind)
                        {
                            case FilterOperationKind.ArraySome:
                                expression = FilterExpressionBuilder.Any(
                                    closureType,
                                    ctx.GetInstance(),
                                    lambda);
                                break;

                            case FilterOperationKind.ArrayNone:
                                expression = FilterExpressionBuilder.Not(
                                    FilterExpressionBuilder.Any(
                                        closureType,
                                        ctx.GetInstance(),
                                        lambda));
                                break;

                            case FilterOperationKind.ArrayAll:
                                expression = FilterExpressionBuilder.All(
                                    closureType,
                                    ctx.GetInstance(),
                                    lambda);
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        if (ctx.InMemory)
                        {
                            expression = FilterExpressionBuilder.NotNullAndAlso(
                                ctx.GetInstance(), expression);
                        }
                        ctx.GetLevel().Enqueue(expression);
                    }
                    ctx.PopInstance();
                }
            }
        }

        private static Type GetTypeFor(FilterOperation operation)
        {
            if (operation.TryGetElementType(out Type? baseType))
            {
                return baseType;
            }
            return operation.Type;
        }
    }
}
