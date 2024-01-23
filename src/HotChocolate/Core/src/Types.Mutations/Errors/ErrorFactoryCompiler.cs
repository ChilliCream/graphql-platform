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

        if (TryCreateDefaultErrorFactory(errorType, out var definitions))
        {
            return definitions;
        }

        if (TryCreateFactoryFromConstructor(errorType, out var definition))
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

        // if none of the above patterns applied it must be an error result type.
        // We will first check if the error was provided as schema type.
        if (ExtendedType.Tools.IsGenericBaseType(errorType) &&
            typeof(ObjectType).IsAssignableFrom(errorType))
        {
            return new[] { new ErrorDefinition(errorType.GetGenericArguments()[0], errorType), };
        }
        
        // else we will create a schema type.
        var schemaType = typeof(ErrorObjectType<>).MakeGenericType(errorType);
        return new[] { new ErrorDefinition(errorType, schemaType), };
    }

    private static bool TryCreateFactoryFromException(
        Type errorType,
        [NotNullWhen(true)] out ErrorDefinition? definition)
    {
        if (typeof(Exception).IsAssignableFrom(errorType))
        {
            var schemaType = typeof(ExceptionObjectType<>).MakeGenericType(errorType);
            definition = new ErrorDefinition(
                errorType,
                schemaType,
                ex => ex.GetType() == errorType
                    ? ex
                    : null);
            return true;
        }

        definition = null;
        return false;
    }

    private static bool TryCreateDefaultErrorFactory(
        Type errorType,
        [NotNullWhen(true)] out ErrorDefinition[]? definitions)
    {
        var getTypeMethod = typeof(Expression)
            .GetMethods()
            .Single(
                t =>
                    t.Name.EqualsOrdinal(nameof(GetType)) &&
                    t.GetParameters().Length == 0);

        const string ex = nameof(ex);

        var exception = Expression.Parameter(typeof(Exception), ex);
        Expression nullValue = Expression.Constant(null, typeof(object));
        List<ErrorDefinition> errorDefinitions = [];

        Expression? instance = null;

        foreach (var methodInfo in errorType
            .GetMethods(Public | Static | Instance)
            .Where(x => x.Name == "CreateErrorFrom"))
        {
            var parameters = methodInfo.GetParameters();

            if (parameters.Length == 1 &&
                typeof(Exception).IsAssignableFrom(parameters[0].ParameterType))
            {
                var resultType = methodInfo.ReturnType == typeof(object)
                    ? errorType
                    : methodInfo.ReturnType;

                var expectedException = parameters[0].ParameterType;

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

                var checkAndCreate = Expression.Condition(
                    test,
                    Expression.Convert(createError, typeof(object)),
                    Expression.Convert(nullValue, typeof(object)));

                var factory =
                    Expression.Lambda<CreateError>(checkAndCreate, exception).Compile();
                var schemaType = typeof(ErrorObjectType<>).MakeGenericType(resultType);
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

        var getTypeMethod = typeof(Expression)
            .GetMethods()
            .Single(
                t =>
                    t.Name.EqualsOrdinal(nameof(GetType)) &&
                    t.GetParameters().Length == 0);

        var exception = Expression.Parameter(typeof(Exception), ex);
        Expression nullValue = Expression.Constant(null, typeof(object));
        var variable = Expression.Variable(typeof(object), obj);
        Expression? previous = null;

        foreach (var constructor in
            errorType.GetConstructors(Public | NonPublic | Instance))
        {
            var parameters = constructor.GetParameters();

            if (parameters.Length == 1 &&
                typeof(Exception).IsAssignableFrom(parameters[0].ParameterType))
            {
                var expectedException = parameters[0].ParameterType;

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
            var factory = Expression.Lambda<CreateError>(
                    Expression.Block(
                        new[]
                        {
                            variable
                        },
                        new List<Expression> { previous, variable, }),
                    exception)
                .Compile();
            var schemaType = typeof(ErrorObjectType<>).MakeGenericType(errorType);
            definition = new ErrorDefinition(errorType, schemaType, factory);
            return true;
        }

        definition = null;
        return false;
    }
}