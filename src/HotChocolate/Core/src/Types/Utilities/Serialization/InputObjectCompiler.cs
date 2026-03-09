using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.Serialization.InputObjectConstructorResolver;

namespace HotChocolate.Utilities.Serialization;

internal static class InputObjectCompiler
{
    private static readonly ParameterExpression s_obj =
        Expression.Parameter(typeof(object), "obj");

    private static readonly ParameterExpression s_fieldValues =
        Expression.Parameter(typeof(object?[]), "fieldValues");

    public static Func<object?[], object> CompileFactory(
        InputObjectType inputType,
        ConstructorInfo? constructor = null)
    {
        var nameSet = TypeMemHelper.RentNameSetOrdinalIgnoreCase();
        var unique = FieldsAreUnique(inputType, nameSet);
        var fields = unique
            ? TypeMemHelper.RentInputFieldMapOrdinalIgnoreCase()
            : TypeMemHelper.RentInputFieldMap();

        if (!unique)
        {
            TypeMemHelper.Return(nameSet);
            nameSet = TypeMemHelper.RentNameSet();
        }

        BuildFieldMap(inputType, fields);

        constructor ??= GetCompatibleConstructor(
            inputType.RuntimeType,
            inputType.Fields,
            fields,
            nameSet);

        var instance = constructor is null
            ? Expression.New(inputType.RuntimeType)
            : CreateInstance(fields, constructor, s_fieldValues);

        if (fields.Count == 0)
        {
            Expression casted = Expression.Convert(instance, typeof(object));
            return Expression.Lambda<Func<object?[], object>>(casted, s_fieldValues).Compile();
        }

        var variable = Expression.Variable(inputType.RuntimeType, "obj");

        var expressions = new List<Expression>
        {
            Expression.Assign(variable, instance)
        };

        CompileSetProperties(variable, fields.Values, s_fieldValues, expressions);
        expressions.Add(Expression.Convert(variable, typeof(object)));
        Expression body = Expression.Block(new[] { variable }, expressions);

        var func = Expression.Lambda<Func<object?[], object>>(body, s_fieldValues).Compile();

        TypeMemHelper.Return(fields);
        TypeMemHelper.Return(nameSet);

        return func;
    }

    public static Func<object?[], object> CompileFactory(
        DirectiveType directiveType,
        ConstructorInfo? constructor = null)
    {
        var nameSet = TypeMemHelper.RentNameSetOrdinalIgnoreCase();
        var unique = FieldsAreUnique(directiveType, nameSet);
        var arguments = unique
            ? TypeMemHelper.RentDirectiveArgumentMapOrdinalIgnoreCase()
            : TypeMemHelper.RentDirectiveArgumentMap();

        if (!unique)
        {
            TypeMemHelper.Return(nameSet);
            nameSet = TypeMemHelper.RentNameSet();
        }

        BuildFieldMap(directiveType, arguments);

        constructor ??= GetCompatibleConstructor(
            directiveType.RuntimeType,
            directiveType.Arguments,
            arguments,
            nameSet);

        var instance = constructor is null
            ? Expression.New(directiveType.RuntimeType)
            : CreateInstance(arguments, constructor, s_fieldValues);

        if (arguments.Count == 0)
        {
            Expression casted = Expression.Convert(instance, typeof(object));
            return Expression.Lambda<Func<object?[], object>>(casted, s_fieldValues).Compile();
        }

        var variable = Expression.Variable(directiveType.RuntimeType, "obj");

        var expressions = new List<Expression>
        {
            Expression.Assign(variable, instance)
        };

        CompileSetProperties(variable, arguments.Values, s_fieldValues, expressions);
        expressions.Add(Expression.Convert(variable, typeof(object)));
        Expression body = Expression.Block([variable], expressions);

        var func = Expression.Lambda<Func<object?[], object>>(body, s_fieldValues).Compile();

        TypeMemHelper.Return(arguments);
        TypeMemHelper.Return(nameSet);

        return func;
    }

    public static Action<object, object?[]> CompileGetFieldValues(InputObjectType inputType)
    {
        Expression instance = s_obj;

        if (inputType.RuntimeType != typeof(object))
        {
            instance = Expression.Convert(instance, inputType.RuntimeType);
        }

        var expressions = new List<Expression>();

        foreach (var field in inputType.Fields.AsSpan())
        {
            var getter = field.Property!.GetGetMethod(true)!;
            Expression fieldValue = Expression.Call(instance, getter);
            expressions.Add(SetFieldValue(field, s_fieldValues, fieldValue));
        }

        Expression body = Expression.Block(expressions);

        return Expression.Lambda<Action<object, object?[]>>(body, s_obj, s_fieldValues).Compile();
    }

    public static Action<object, object?[]> CompileGetFieldValues(DirectiveType inputType)
    {
        Expression instance = s_obj;

        if (inputType.RuntimeType != typeof(object))
        {
            instance = Expression.Convert(instance, inputType.RuntimeType);
        }

        var expressions = new List<Expression>();

        foreach (var field in inputType.Arguments.AsSpan())
        {
            var getter = field.Property!.GetGetMethod(true)!;
            Expression fieldValue = Expression.Call(instance, getter);
            expressions.Add(SetFieldValue(field, s_fieldValues, fieldValue));
        }

        Expression body = Expression.Block(expressions);

        return Expression.Lambda<Action<object, object?[]>>(body, s_obj, s_fieldValues).Compile();
    }

    private static Expression CreateInstance<T>(
        Dictionary<string, T> fields,
        ConstructorInfo constructor,
        Expression fieldValues)
        where T : class, IInputValueDefinition, IPropertyProvider, IRuntimeTypeProvider, IFieldIndexProvider
        => Expression.New(
            constructor,
            CompileAssignParameters(fields, constructor, fieldValues));

    private static Expression[] CompileAssignParameters<T>(
        Dictionary<string, T> fields,
        ConstructorInfo constructor,
        Expression fieldValues)
        where T : class, IInputValueDefinition, IPropertyProvider, IRuntimeTypeProvider, IFieldIndexProvider
    {
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return [];
        }

        var expressions = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (fields.TryGetParameter(parameter, out var field))
            {
                fields.Remove(field.Property!.Name);
                var value = GetFieldValue(field, fieldValues);

                if (field is InputField { IsOptional: true })
                {
                    value = CreateOptional(value, field.RuntimeType);
                }
                else if (parameter.ParameterType.IsValueType
                    && System.Nullable.GetUnderlyingType(parameter.ParameterType) == null)
                {
                    value = Expression.Coalesce(value, Expression.Default(parameter.ParameterType));
                }

                expressions[i] = Expression.Convert(value, parameter.ParameterType);
            }
            else if (parameter.HasDefaultValue)
            {
                if (parameter.DefaultValue is { } || !parameter.ParameterType.IsValueType)
                {
                    expressions[i] = Expression.Constant(parameter.DefaultValue, parameter.ParameterType);
                }
                else
                {
                    expressions[i] = Expression.Default(parameter.ParameterType);
                }
            }
            else
            {
                throw new InvalidOperationException("Could not resolver parameter.");
            }
        }

        return expressions;
    }

    private static void CompileSetProperties<T>(
        Expression instance,
        IEnumerable<T> fields,
        Expression fieldValues,
        List<Expression> currentBlock)
        where T : IInputValueDefinition, IPropertyProvider, IFieldIndexProvider, IRuntimeTypeProvider
    {
        foreach (var field in fields)
        {
            var setter = field.Property!.GetSetMethod(true)!;
            var value = GetFieldValue(field, fieldValues);

            if (field is InputField { IsOptional: true })
            {
                value = CreateOptional(value, field.RuntimeType);
            }
            else if (field.Property.PropertyType.IsValueType
                && System.Nullable.GetUnderlyingType(field.Property.PropertyType) == null)
            {
                value = Expression.Coalesce(value, Expression.Default(field.Property.PropertyType));
            }

            value = Expression.Convert(value, field.Property.PropertyType);
            Expression setPropertyValue = Expression.Call(instance, setter, value);
            currentBlock.Add(setPropertyValue);
        }
    }

    private static Expression GetFieldValue<T>(T field, Expression fieldValues)
        where T : IInputValueDefinition, IPropertyProvider, IFieldIndexProvider
        => Expression.ArrayIndex(fieldValues, Expression.Constant(field.Index));

    private static Expression SetFieldValue<T>(
        T field,
        Expression fieldValues,
        Expression fieldValue)
        where T : IInputValueDefinition, IFieldIndexProvider
    {
        Expression index = Expression.Constant(field.Index);
        Expression element = Expression.ArrayAccess(fieldValues, index);
        Expression casted = Expression.Convert(fieldValue, typeof(object));
        return Expression.Assign(element, casted);
    }

    private static bool FieldsAreUnique(
        InputObjectType type,
        HashSet<string> nameSetIgnoreCase)
    {
        var unique = true;

        foreach (var field in type.Fields.AsSpan())
        {
            if (!nameSetIgnoreCase.Add(field.Property!.Name))
            {
                unique = false;
                break;
            }
        }

        return unique;
    }

    private static bool FieldsAreUnique(
        DirectiveType type,
        HashSet<string> nameSetIgnoreCase)
    {
        var unique = true;

        foreach (var field in type.Arguments.AsSpan())
        {
            if (!nameSetIgnoreCase.Add(field.Property!.Name))
            {
                unique = false;
                break;
            }
        }

        return unique;
    }

    private static void BuildFieldMap(
        InputObjectType type,
        Dictionary<string, InputField> fields)
    {
        foreach (var field in type.Fields.AsSpan())
        {
            fields.Add(field.Property!.Name, field);
        }
    }

    private static void BuildFieldMap(
        DirectiveType type,
        Dictionary<string, DirectiveArgument> fields)
    {
        foreach (var field in type.Arguments.AsSpan())
        {
            fields.Add(field.Property!.Name, field);
        }
    }

    private static Expression CreateOptional(Expression fieldValue, Type runtimeType)
    {
        var from =
            typeof(Optional<>)
                .MakeGenericType(runtimeType)
                .GetMethod("From", BindingFlags.Public | BindingFlags.Static)!;
        Debug.Assert(from is not null, "From helper on Optional<T> is missing.");
        fieldValue = Expression.Convert(fieldValue, typeof(IOptional));
        return Expression.Call(from, fieldValue);
    }
}
