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
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                          || field.Operation.Kind == FilterOperationKind.ArrayNone
                          || field.Operation.Kind == FilterOperationKind.ArrayAll)
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

                    context.AddScope();

                    if (node.Value.IsNull())
                    {
                        context.GetLevel().Enqueue(
                            FilterExpressionBuilder.Equals(ctx.GetClosure().Parameter, null));

                        action = SyntaxVisitor.SkipAndLeave;
                    }
                    else
                    {
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
            IFilterVisitorContext<Expression> ctx)
        {
            if (ctx is QueryableFilterVisitorContext context)
            {
                if (field.Operation.Kind == FilterOperationKind.ArraySome
                    || field.Operation.Kind == FilterOperationKind.ArrayNone
                    || field.Operation.Kind == FilterOperationKind.ArrayAll)
                {
                    FilterScope<Expression> nestedScope = context.PopScope();

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
                                    context.GetInstance(),
                                    lambda);
                                break;

                            case FilterOperationKind.ArrayNone:
                                expression = FilterExpressionBuilder.Not(
                                    FilterExpressionBuilder.Any(
                                        closureType,
                                        context.GetInstance(),
                                        lambda));
                                break;

                            case FilterOperationKind.ArrayAll:
                                expression = FilterExpressionBuilder.All(
                                    closureType,
                                    context.GetInstance(),
                                    lambda);
                                break;

                            default:
                                throw new NotSupportedException();
                        }

                        if (context.InMemory)
                        {
                            expression = FilterExpressionBuilder.NotNullAndAlso(
                                context.GetInstance(), expression);
                        }
                        context.GetLevel().Enqueue(expression);
                    }
                    context.PopInstance();
                }
            }
        }

        private static Type GetTypeFor(FilterOperation operation)
        {
            if (operation.TryGetSimpleFilterBaseType(out Type? baseType))
            {
                return baseType;
            }
            return operation.Type;
        }
    }
}
