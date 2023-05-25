using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

public static class QueryableProjectionScopeExtensions
{
    /// <summary>
    /// Creates an expression based on the result stored on <see cref="QueryableProjectionScope"/>.
    /// </summary>
    /// <param name="scope">The scope that contains the projection information</param>
    /// <typeparam name="T">The target type</typeparam>
    /// <returns>An expression</returns>
    public static Expression<Func<T, object[]>> Project<T>(this QueryableProjectionScope scope)
        => (Expression<Func<T, object[]>>)scope.CreateMemberInitLambda();

    /// <summary>
    /// Creates an expression based on the result stored on <see cref="QueryableProjectionScope"/>.
    /// Casts the result onto <typeparamref name="TTarget"/> in the lambda
    /// </summary>
    /// <param name="scope">The scope that contains the projection information</param>
    /// <typeparam name="T">The target type</typeparam>
    /// <typeparam name="TTarget">The target result type of the expression</typeparam>
    /// <returns></returns>
    public static Expression<Func<T, TTarget>> Project<T, TTarget>(
        this QueryableProjectionScope scope)
        where T : TTarget
        => (Expression<Func<T, TTarget>>)scope.CreateMemberInitLambda<TTarget>();

    public static Expression CreateMemberInit(this QueryableProjectionScope scope)
    {
        if (scope.HasAbstractTypes())
        {
            Expression lastValue = Expression.Default(typeof(object[]));

            foreach (var (type, initializers) in scope.GetAbstractTypes())
            {
                Expression memberInit = Expression.NewArrayInit(typeof(object), initializers);

                lastValue = Expression.Condition(
                    Expression.TypeIs(scope.Instance.Peek(), type),
                    memberInit,
                    lastValue);
            }

            return lastValue;
        }
        else
        {
            return Expression.NewArrayInit(typeof(object), scope.Level.Peek());
        }
    }

    public static Expression CreateMemberInitLambda(this QueryableProjectionScope scope)
    {
        return Expression.Lambda(scope.CreateMemberInit(), scope.Parameter);
    }

    private static Expression CreateMemberInitLambda<T>(this QueryableProjectionScope scope)
    {
        Expression converted = Expression.Convert(scope.CreateMemberInit(), typeof(T));
        return Expression.Lambda(converted, scope.Parameter);
    }

    public static Expression CreateSelection(
        this QueryableProjectionScope scope,
        Expression source,
        Type sourceType)
    {
        var selection = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            new[] { scope.RuntimeType, typeof(object[]), },
            source,
            scope.CreateMemberInitLambda());

        return selection;
        /*
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.ToList),
            new[]
            {
                typeof(object[])
            },
            selection);

        if (sourceType.IsArray)
        {
            return ToArray(scope, selection);
        }

        if (TryGetSetType(sourceType, out var setType))
        {
            return ToSet(selection, setType);
        }

        return ToList(scope, selection);
    */
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
        var typedGeneric =
            setType.MakeGenericType(source.Type.GetGenericArguments()[0]);

        var ctor =
            typedGeneric.GetConstructor(new[] { source.Type });

        if (ctor is null)
        {
            throw ThrowHelper.ProjectionVisitor_NoConstructorFoundForSet(source, setType);
        }

        return Expression.New(ctor, source);
    }

    private static bool TryGetSetType(
        Type type,
        [NotNullWhen(true)] out Type? setType)
    {
        if (type.IsGenericType)
        {
            var typeDefinition = type.GetGenericTypeDefinition();
            if (typeDefinition == typeof(ISet<>) ||
                typeDefinition == typeof(HashSet<>))
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
