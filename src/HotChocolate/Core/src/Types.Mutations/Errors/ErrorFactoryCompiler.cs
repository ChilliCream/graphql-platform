using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using static System.Reflection.BindingFlags;

namespace HotChocolate.Types;

internal static class ErrorFactoryCompiler
{
    public static IReadOnlyList<ErrorDefinition> Compile(Type errorType)
    {
        if (errorType is null)
        {
            throw new ArgumentNullException(nameof(errorType));
        }

        if (TryCreateDefaultErrorFactory(errorType, out ErrorDefinition[]? definitions))
        {
            return definitions;
        }

        if (TryCreateFactoryFromConstructor(errorType, out ErrorDefinition? definition))
        {
            return new[]
            {
                definition
            };
        }

        if (TryCreateFactoryFromException(errorType, out definition))
        {
            return new[]
            {
                definition
            };
        }

        throw ThrowHelper.TypeDoesNotExposeErrorFactory(errorType);
    }

    private static bool TryCreateFactoryFromException(
        Type errorType,
        [NotNullWhen(true)] out ErrorDefinition? definition)
    {
        if (typeof(Exception).IsAssignableFrom(errorType))
        {
            Type schemaType = typeof(ExceptionObjectType<>).MakeGenericType(errorType);
            definition = new ErrorDefinition(
                errorType,
                schemaType,
                ex => ex.GetType() == errorType ? ex : null);
            return true;
        }

        definition = null;
        return false;
    }

    private static bool TryCreateDefaultErrorFactory(
        Type errorType,
        [NotNullWhen(true)] out ErrorDefinition[]? definitions)
    {
        MethodInfo getTypeMethod = typeof(Expression)
            .GetMethods()
            .Single(t =>
                t.Name.EqualsOrdinal(nameof(GetType)) &&
                t.GetParameters().Length == 0);

        const string ex = nameof(ex);

        ParameterExpression exception = Expression.Parameter(typeof(Exception), ex);
        Expression nullValue = Expression.Constant(null, typeof(object));
        List<ErrorDefinition> errorDefinitions = new();

        Expression? instance = null;
        foreach (MethodInfo methodInfo in errorType
            .GetMethods(Public | Static | Instance)
            .Where(x => x.Name == "CreateErrorFrom"))
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            if (parameters.Length == 1 &&
                typeof(Exception).IsAssignableFrom(parameters[0].ParameterType))
            {
                Type resultType = methodInfo.ReturnType == typeof(object)
                    ? errorType
                    : methodInfo.ReturnType;

                Type expectedException = parameters[0].ParameterType;

                Expression expected = Expression.Constant(expectedException, typeof(Type));
                Expression actual = Expression.Call(exception, getTypeMethod);
                Expression test = Expression.Equal(expected, actual);

                Expression castedException = Expression.Convert(exception, expectedException);

                Expression createError;
                if (methodInfo.IsStatic)
                {
                    createError = Expression.Call(methodInfo, castedException);
                }
                else
                {
                    instance ??= Expression.Constant(Activator.CreateInstance(errorType));
                    createError = Expression.Call(instance, methodInfo, castedException);
                }

                ConditionalExpression checkAndCreate = Expression.Condition(
                    test,
                    Expression.Convert(createError, typeof(object)),
                    Expression.Convert(nullValue, typeof(object)));

                CreateError factory =
                    Expression.Lambda<CreateError>(checkAndCreate, exception).Compile();
                Type schemaType = typeof(ErrorObjectType<>).MakeGenericType(resultType);
                errorDefinitions.Add(new ErrorDefinition(resultType, schemaType, factory));
            }
        }

        definitions = errorDefinitions.ToArray();
        return definitions.Length > 0;
    }

    private static bool TryCreateFactoryFromConstructor(
        Type errorType,
        [NotNullWhen(true)] out ErrorDefinition? definition)
    {
        const string ex = nameof(ex);
        const string obj = nameof(obj);

        MethodInfo getTypeMethod = typeof(Expression)
            .GetMethods()
            .Single(t =>
                t.Name.EqualsOrdinal(nameof(GetType)) &&
                t.GetParameters().Length == 0);

        ParameterExpression exception = Expression.Parameter(typeof(Exception), ex);
        Expression nullValue = Expression.Constant(null, typeof(object));
        ParameterExpression variable = Expression.Variable(typeof(object), obj);
        Expression? previous = null;

        foreach (ConstructorInfo constructor in
            errorType.GetConstructors(Public | NonPublic | Instance))
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length == 1 &&
                typeof(Exception).IsAssignableFrom(parameters[0].ParameterType))
            {
                Type expectedException = parameters[0].ParameterType;

                Expression expected = Expression.Constant(expectedException, typeof(Type));
                Expression actual = Expression.Call(exception, getTypeMethod);
                Expression test = Expression.Equal(expected, actual);

                Expression castedException = Expression.Convert(exception, expectedException);
                Expression createError = Expression.New(constructor, castedException);

                previous =
                    Expression.IfThenElse(
                        test,
                        Expression.Assign(variable, createError),
                        previous is null
                            ? Expression.Assign(variable, nullValue)
                            : Expression.Assign(variable, previous));
            }
        }

        if (previous is not null)
        {
            CreateError factory = Expression.Lambda<CreateError>(
                    Expression.Block(
                        new[]
                        {
                            variable
                        },
                        new List<Expression> { previous, variable }),
                    exception)
                .Compile();
            Type schemaType = typeof(ErrorObjectType<>).MakeGenericType(errorType);
            definition = new ErrorDefinition(errorType, schemaType, factory);
            return true;
        }

        definition = null;
        return false;
    }
}
