using System.Diagnostics.CodeAnalysis;
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
        // When the type exposes non-public parameterized constructors (e.g., EF
        // constructor service injection), we must preserve the instance that EF
        // materialized so that injected services are not lost.
        if (ShouldReuseExistingInstance(scope.RuntimeType))
        {
            return scope.Instance.Peek();
        }

        if (scope.HasAbstractTypes())
        {
            Expression lastValue = Expression.Default(scope.RuntimeType);

            foreach (var val in scope.GetAbstractTypes())
            {
                var memberInit = CreateProjectedInstance(val.Key, val.Value);

                lastValue = Expression.Condition(
                    Expression.TypeIs(scope.Instance.Peek(), val.Key),
                    Expression.Convert(memberInit, scope.RuntimeType),
                    lastValue);
            }

            return lastValue;
        }
        else
        {
            return CreateProjectedInstance(scope.RuntimeType, scope.Level.Peek());
        }
    }

    private static bool ShouldReuseExistingInstance(Type type)
        => type.GetConstructor(Type.EmptyTypes) is not null
            && type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Any(t => t.GetParameters().Length > 0);

    private static Expression CreateProjectedInstance(
        Type runtimeType,
        IEnumerable<MemberAssignment> assignments)
    {
        var bindings = assignments.ToArray();

        if (runtimeType.GetConstructor(Type.EmptyTypes) is { } parameterlessCtor)
        {
            return Expression.MemberInit(Expression.New(parameterlessCtor), bindings);
        }

        ConstructorProjection? best = null;
        foreach (var constructor in runtimeType.GetConstructors())
        {
            if (!TryCreateProjection(constructor, bindings, out var projection))
            {
                continue;
            }

            if (best is null
                || projection.MatchedParameterCount > best.MatchedParameterCount
                || (projection.MatchedParameterCount == best.MatchedParameterCount
                    && projection.UnmatchedBindingCount < best.UnmatchedBindingCount)
                || (projection.MatchedParameterCount == best.MatchedParameterCount
                    && projection.UnmatchedBindingCount == best.UnmatchedBindingCount
                    && projection.ParameterCount < best.ParameterCount))
            {
                best = projection;
            }
        }

        if (best is not null)
        {
            return best.Expression;
        }

        // Preserve the existing exception shape if no suitable constructor exists.
        return Expression.MemberInit(Expression.New(runtimeType), bindings);
    }

    private static bool TryCreateProjection(
        ConstructorInfo constructor,
        MemberAssignment[] bindings,
        [NotNullWhen(true)] out ConstructorProjection? projection)
    {
        var parameters = constructor.GetParameters();
        var arguments = new Expression[parameters.Length];
        var consumed = new bool[bindings.Length];
        var matched = 0;

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var parameter = parameters[parameterIndex];
            var bindingIndex = FindMatchingBinding(parameter, bindings, consumed);

            if (bindingIndex >= 0)
            {
                consumed[bindingIndex] = true;
                matched++;
                arguments[parameterIndex] = CastIfNeeded(
                    bindings[bindingIndex].Expression,
                    parameter.ParameterType);
            }
            else if (parameter.HasDefaultValue)
            {
                arguments[parameterIndex] = CreateDefaultValueExpression(parameter);
            }
            else
            {
                arguments[parameterIndex] = Expression.Default(parameter.ParameterType);
            }
        }

        List<MemberAssignment>? unmatchedBindings = null;
        var unmatchedCount = 0;
        for (var bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
        {
            if (consumed[bindingIndex])
            {
                continue;
            }

            if (!CanAssign(bindings[bindingIndex].Member))
            {
                projection = null;
                return false;
            }

            unmatchedBindings ??= [];
            unmatchedBindings.Add(bindings[bindingIndex]);
            unmatchedCount++;
        }

        var constructorExpression = Expression.New(constructor, arguments);
        Expression expression =
            unmatchedBindings is null
                ? constructorExpression
                : Expression.MemberInit(constructorExpression, unmatchedBindings);

        projection = new ConstructorProjection(
            expression,
            matched,
            unmatchedCount,
            parameters.Length);

        return true;
    }

    private static int FindMatchingBinding(
        ParameterInfo parameter,
        MemberAssignment[] bindings,
        bool[] consumed)
    {
        for (var bindingIndex = 0; bindingIndex < bindings.Length; bindingIndex++)
        {
            if (consumed[bindingIndex])
            {
                continue;
            }

            var binding = bindings[bindingIndex];
            if (!string.Equals(binding.Member.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsCompatibleType(binding.Expression.Type, parameter.ParameterType))
            {
                return bindingIndex;
            }
        }

        return -1;
    }

    private static bool IsCompatibleType(Type sourceType, Type targetType)
    {
        if (targetType.IsAssignableFrom(sourceType))
        {
            return true;
        }

        try
        {
            _ = Expression.Convert(Expression.Default(sourceType), targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Expression CreateDefaultValueExpression(ParameterInfo parameter)
    {
        if (parameter.DefaultValue is null)
        {
            return Expression.Default(parameter.ParameterType);
        }

        var defaultValue = parameter.DefaultValue;
        var constant = Expression.Constant(defaultValue, defaultValue.GetType());
        return CastIfNeeded(constant, parameter.ParameterType);
    }

    private static Expression CastIfNeeded(Expression source, Type targetType)
        => source.Type == targetType
            ? source
            : Expression.Convert(source, targetType);

    private static bool CanAssign(MemberInfo member)
        => member switch
        {
            PropertyInfo { CanWrite: true } => true,
            FieldInfo { IsInitOnly: false } => true,
            _ => false
        };

    private sealed record ConstructorProjection(
        Expression Expression,
        int MatchedParameterCount,
        int UnmatchedBindingCount,
        int ParameterCount);

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
