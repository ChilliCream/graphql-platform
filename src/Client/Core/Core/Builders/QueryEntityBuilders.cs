using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Client.Core.Builders
{
    public static class QueryEntityBuilders
    {
        public static IQueryableList<TValue> CreateMethodCall<TObject, TValue>(
            this TObject o,
            Expression<Func<TObject, IQueryableList<TValue>>> selector)
                where TObject : IQueryableValue
        {
            var methodCall = (MethodCallExpression)selector.Body;
            var arguments = new List<ConstantExpression>();

            foreach (MemberExpression arg in methodCall.Arguments)
            {
                var expression = (ConstantExpression)arg.Expression;
                var value = ((FieldInfo)arg.Member).GetValue(expression.Value);
                arguments.Add(Expression.Constant(value, arg.Type));
            }

            return new QueryableList<TValue>(
                Expression.Call(
                    Expression.Constant(o),
                    methodCall.Method,
                    arguments));
        }

        public static IQueryableValue<TValue> CreateMethodCall<TObject, TValue>(
            this TObject o,
            Expression<Func<TObject, IQueryableValue<TValue>>> selector)
                where TObject : IQueryableValue
        {
            var methodCall = (MethodCallExpression)selector.Body;
            var arguments = new List<ConstantExpression>();

            foreach (MemberExpression arg in methodCall.Arguments)
            {
                var expression = (ConstantExpression)arg.Expression;
                var value = ((FieldInfo)arg.Member).GetValue(expression.Value);
                arguments.Add(Expression.Constant(value, arg.Type));
            }

            return new QueryableValue<TValue>(
                Expression.Call(
                    Expression.Constant(o),
                    methodCall.Method,
                    arguments));
        }

        public static TValue CreateMethodCall<TObject, TValue>(
            this TObject o,
            Expression<Func<TObject, TValue>> selector,
            Func<Expression, TValue> create)
                where TObject : IQueryableValue
                where TValue : IQueryableValue
        {
            var methodCall = (MethodCallExpression)selector.Body;
            var arguments = new List<ConstantExpression>();

            foreach (MemberExpression arg in methodCall.Arguments)
            {
                var expression = (ConstantExpression)arg.Expression;
                var value = ((FieldInfo)arg.Member).GetValue(expression.Value);
                arguments.Add(Expression.Constant(value, arg.Type));
            }

            return create(
                Expression.Call(
                    Expression.Constant(o),
                    methodCall.Method,
                    arguments));
        }

        public static TValue CreateProperty<TObject, TValue>(
            this TObject o,
            Expression<Func<TObject, TValue>> selector,
            Func<Expression, TValue> create)
                where TObject : IQueryableValue
        {
            return create(
                Expression.Property(
                    Expression.Constant(o),
                    (PropertyInfo)((MemberExpression)selector.Body).Member));
        }

        public static IQueryableList<TValue> CreateProperty<TObject, TValue>(
            this TObject o,
            Expression<Func<TObject, IQueryableList<TValue>>> selector)
                where TObject : IQueryableValue
                where TValue : IQueryableValue
        {
            return new QueryableList<TValue>(
                Expression.Property(
                    Expression.Constant(o),
                    (PropertyInfo)((MemberExpression)selector.Body).Member));
        }
    }
}
