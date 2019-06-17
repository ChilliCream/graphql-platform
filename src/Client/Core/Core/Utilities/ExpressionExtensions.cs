using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using HotChocolate.Client.Core.Builders;
using HotChocolate.Language;

namespace HotChocolate.Client.Core.Utilities
{
    /// <summary>
    /// Extension methods for Expression objects.
    /// </summary>
    static class ExpressionExtensions
    {
        /// <summary>
        /// Remove all the outer quotes from an expression.
        /// </summary>
        /// <param name="expression">Expression that might contain outer quotes.</param>
        /// <returns>Expression that no longer contains outer quotes.</returns>
        public static Expression StripQuotes(this Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        /// <summary>
        /// Get the lambda for an expression stripping any necessary outer quotes.
        /// </summary>
        /// <param name="expression">Expression that should be a lamba possibly wrapped
        /// in outer quotes.</param>
        /// <returns>LambdaExpression no longer wrapped in quotes.</returns>
        public static LambdaExpression GetLambda(this Expression expression)
        {
            return (LambdaExpression)expression.StripQuotes();
        }

        public static Expression AddCast(this Expression expression, Type type)
        {
            var sourceType = expression.Type.GetTypeInfo();
            var targetType = type.GetTypeInfo();

            if (targetType == sourceType)
            {
                return expression;
            }
            else if (targetType.IsAssignableFrom(sourceType))
            {
                return Expression.Convert(expression, type);
            }
            else if (typeof(JToken).GetTypeInfo().IsAssignableFrom(sourceType))
            {
                // If the source type is a JToken use JToken.ToObject to convert to the target type.
                return Expression.Call(
                    expression,
                    JsonMethods.JTokenToObject.MakeGenericMethod(type));
            }
            else if (IsIEnumerableOfJToken(sourceType))
            {
                // If the source type is an IEnumerable<JToken> then add a select statement to cast.
                return AddSelectCast(expression, type);
            }

            throw new NotSupportedException(
                $"Don't know how to cast '{expression}' ({expression.Type}) to '{type}'.");
        }

        /// <summary>
        /// Adds an indexer to an expression representing a <see cref="JToken"/>.
        /// </summary>
        /// <param name="instance">The expression.</param>
        /// <param name="field">The field to return.</param>
        /// <returns>A new expression.</returns>
        public static Expression AddIndexer(this Expression instance, FieldNode field)
        {
            return AddIndexer(instance, (field.Alias ?? field.Name).Value);
        }

        /// <summary>
        /// Adds an indexer to an expression representing a <see cref="JToken"/> or a collection of
        /// <see cref="JToken"/>s.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="fieldName">The field to return.</param>
        /// <returns>A new expression.</returns>
        public static IndexExpression AddIndexer(this Expression expression, string fieldName)
        {
            if (typeof(JToken).GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
            {
                return Expression.Property(expression, JsonMethods.JTokenIndexer, Expression.Constant(fieldName));
            }
            else
            {
                throw new NotSupportedException($"Don't know how to add an indexer to {expression}.");
            }
        }

        public static Expression AddToList(this Expression expression)
        {
            var itemType = GetEnumerableItemType(expression.Type);

            if (itemType == null)
            {
                throw new NotSupportedException(
                    $"Don't know how to call ToList on '{expression}' ({expression.Type}).");
            }

            return Expression.Call(
                LinqMethods.ToListMethod.MakeGenericMethod(itemType),
                expression);
        }


        private static Expression AddSelectCast(Expression expression, Type type)
        {
            var targetItemTypeType = GetEnumerableItemType(type);

            if (targetItemTypeType != null)
            {
                // The target type is an IEnumerable<>.
                var genericTypeArguments = type.GetTypeInfo().GenericTypeArguments;
                var queryType = genericTypeArguments[0];

                if (expression is MethodCallExpression methodCall)
                {
                    if (IsSelect(methodCall.Method))
                    {
                        // The source expression is an ExpressionMethods.Select call. Create a new
                        // ExpressionMethods.Select call with a modified selector lambda which adds
                        // the required cast.
                        // TODO: Is this needed? There are no covering tests.
                        var instance = methodCall.Arguments[0];
                        var lambda = methodCall.Arguments[1].GetLambda();
                        return Expression.Call(
                            methodCall.Method.GetGenericMethodDefinition().MakeGenericMethod(queryType),
                            instance,
                            Expression.Lambda(
                                lambda.Body.AddCast(queryType),
                                lambda.Parameters));
                    }
                }
            }

            throw new NotSupportedException(
                $"Don't know how to cast '{expression}' ({expression.Type}) to '{type}'.");
        }

        private static Type GetEnumerableItemType(Type type)
        {
            if (type == typeof(string))
                return null;

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetTypeInfo().GenericTypeArguments[0];
            }

            foreach (var i in type.GetTypeInfo().ImplementedInterfaces)
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return i.GetTypeInfo().GenericTypeArguments[0];
                }
            }

            return null;
        }

        private static bool IsIEnumerableOfJToken(TypeInfo type)
        {
            return typeof(IEnumerable<JToken>).GetTypeInfo().IsAssignableFrom(type);
        }

        private static bool IsSelect(MethodInfo method)
        {
            return method.GetGenericMethodDefinition() == Rewritten.List.SelectMethod ||
                method.GetGenericMethodDefinition() == Rewritten.Value.SelectMethod;
        }
    }
}
