using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions;

public static class QueryableProjectionScopeExtensions
{
    /// <summary>
    /// Creates an expression based on the result stored on <see cref="QueryableProjectionScope"/>.
    /// </summary>
    /// <param name="scope">The scope that contains the projection information</param>
    /// <typeparam name="T">The target type</typeparam>
    /// <returns>An expression</returns>
    public static Expression<Func<T, T>> Project<T>(this QueryableProjectionScope scope)
        => (Expression<Func<T, T>>)scope.CreateMemberInitLambda();

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
            Expression lastValue = Expression.Default(scope.RuntimeType);

            foreach (var val in scope.GetAbstractTypes())
            {
                var ctor = Expression.New(val.Key);
                Expression memberInit = Expression.MemberInit(ctor, val.Value);

                lastValue = Expression.Condition(
                    Expression.TypeIs(scope.Instance.Peek(), val.Key),
                    Expression.Convert(memberInit, scope.RuntimeType),
                    lastValue);
            }

            return lastValue;
        }
        else
        {
            var ctor = Expression.New(scope.RuntimeType);
            return Expression.MemberInit(ctor, scope.Level.Peek());
        }
    }

    public static Expression CreateMemberInitLambda(this QueryableProjectionScope scope)
        => Expression.Lambda(scope.CreateMemberInit(), scope.Parameter);

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
        var elementType = GetElementType(sourceType) ?? scope.RuntimeType;
        var selector = CreateMemberInitLambda(scope, elementType);

        var selection = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Select),
            [
                scope.RuntimeType,
                elementType
            ],
            source,
            selector);

        if (sourceType.IsArray)
        {
            return ToArray(selection, elementType);
        }

        if (TryGetSetType(sourceType, out var setType))
        {
            return ToSet(selection, setType);
        }

        return ToList(selection, elementType);
    }

    private static Expression CreateMemberInitLambda(
        QueryableProjectionScope scope,
        Type targetType)
    {
        var projection = scope.CreateMemberInit();
        if (targetType != scope.RuntimeType)
        {
            projection = Expression.Convert(projection, targetType);
        }

        return Expression.Lambda(projection, scope.Parameter);
    }

    private static Expression ToArray(Expression source, Type elementType)
    {
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.ToArray),
            [
                elementType
            ],
            source);
    }

    private static Expression ToList(Expression source, Type elementType)
    {
        return Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.ToList),
            [
                elementType
            ],
            source);
    }

    private static Expression ToSet(
        Expression source,
        Type setType)
    {
        var typedGeneric =
            setType.MakeGenericType(source.Type.GetGenericArguments()[0]);

        var ctor =
            typedGeneric.GetConstructor(
            [
                source.Type
            ]);

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

        setType = null;
        return false;
    }

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }
}
