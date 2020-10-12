using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions
{
    public static class QueryableProjectionScopeExtensions
    {
        public static Expression<Func<T, T>> Project<T>(this QueryableProjectionScope scope)
        {
            return (Expression<Func<T, T>>)scope.CreateMemberInitLambda();
        }

        public static MemberInitExpression CreateMemberInit(this QueryableProjectionScope scope)
        {
            NewExpression ctor = Expression.New(scope.RuntimeType);
            return Expression.MemberInit(ctor, scope.Level.Peek());
        }

        public static Expression CreateMemberInitLambda(this QueryableProjectionScope scope)
        {
            return Expression.Lambda(scope.CreateMemberInit(), scope.Parameter);
        }

        public static Expression CreateSelection(
            this QueryableProjectionScope scope,
            Expression source,
            Type sourceType)
        {
            MethodCallExpression selection = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Select),
                new[] { scope.RuntimeType, scope.RuntimeType },
                source,
                scope.CreateMemberInitLambda());

            if (sourceType.IsArray)
            {
                return ToArray(scope, selection);
            }

            if (TryGetSetType(sourceType, out Type? setType))
            {
                return ToSet(selection, setType);
            }

            return ToList(scope, selection);
        }

        private static Expression ToArray(QueryableProjectionScope scope, Expression source)
        {
            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.ToArray),
                new[] { scope.RuntimeType },
                source);
        }

        private static Expression ToList(QueryableProjectionScope scope, Expression source)
        {
            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.ToList),
                new[] { scope.RuntimeType },
                source);
        }

        private static Expression ToSet(
            Expression source,
            Type setType)
        {
            Type typedGeneric =
                setType.MakeGenericType(source.Type.GetGenericArguments()[0]);

            ConstructorInfo? ctor =
                typedGeneric.GetConstructor(new[] { source.Type });

            if (ctor is null)
            {
                throw new InvalidOperationException();
            }

            return Expression.New(ctor, source);
        }

        private static bool TryGetSetType(
            Type type,
            [NotNullWhen(true)] out Type? setType)
        {
            if (type.IsGenericType)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(ISet<>)
                    || typeDefinition == typeof(HashSet<>))
                {
                    setType = typeof(HashSet<>);
                    return true;
                }

                if (typeDefinition == typeof(SortedSet<>))
                {
                    setType = typeof(SortedSet<>);
                    return true;
                }
            }

            setType = default;
            return false;
        }
    }
}
