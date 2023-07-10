using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes.Execution;

public static class ProjectionReflectionHelper
{
    private static MethodInfo GetGenericSelect(Type type)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
            {
                if (m.Name != "Select")
                    return false;

                var parameters = m.GetParameters();
                if (parameters.Length != 2)
                    return false;

                Type[] genericArgsToFunc;
                if (type == typeof(Enumerable))
                {
                    var func = parameters[1];
                    genericArgsToFunc = func.ParameterType.GetGenericArguments();
                }
                else if (type == typeof(Queryable))
                {
                    var expr = parameters[1];
                    var func = expr.ParameterType.GetGenericArguments()[0];
                    genericArgsToFunc = func.GetGenericArguments();
                }
                else
                {
                    throw new InvalidOperationException($"Wrong type {type}");
                }

                return genericArgsToFunc.Length == 2;
            });
    }

    public static readonly MethodInfo EnumerableSelect = GetGenericSelect(typeof(Enumerable));
    public static readonly MethodInfo QueryableSelectWithoutIndexMethod = GetGenericSelect(typeof(Queryable));

    public static IQueryable SelectT<T>(this IQueryable<T> query, LambdaExpression expression)
        => SelectT((IQueryable) query, expression);

    public static IQueryable SelectT(this IQueryable query, LambdaExpression expression)
    {
        var outputType = expression.ReturnType;
        var inputType = query.ElementType;
        var genericSelect = QueryableSelectWithoutIndexMethod.MakeGenericMethod(
            inputType, outputType);
        var methodCallExpression = Expression.Call(null, genericSelect, query.Expression, expression);
        var query1 = query.Provider.CreateQuery(methodCallExpression);
        return query1;
    }

    public static IEnumerable SelectT(this IEnumerable enumerable, Delegate func)
    {
        var inputType = enumerable
            .GetType()
            .GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .GetGenericArguments()[0];
        return SelectT(enumerable, inputType, func);
    }

    public static IEnumerable SelectT<T>(this IEnumerable<T> enumerable, Delegate func)
        => SelectT(enumerable, typeof(T), func);

    private static IEnumerable SelectT(IEnumerable enumerable, Type inputType, Delegate func)
    {
        var outputType = func.GetType().GetGenericArguments()[1];
        var genericSelect = EnumerableSelect.MakeGenericMethod(inputType, outputType);
        var result = genericSelect.Invoke(null, new object?[] { enumerable, func })!;
        return (IEnumerable) result;
    }
}
