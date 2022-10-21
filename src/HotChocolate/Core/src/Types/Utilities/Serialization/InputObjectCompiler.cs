using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.Serialization.InputObjectConstructorResolver;

#nullable enable

namespace HotChocolate.Utilities.Serialization;

internal static class InputObjectCompiler
{
    private static readonly ParameterExpression _obj =
        Expression.Parameter(typeof(object), "obj");
    private static readonly ParameterExpression _fieldValues =
        Expression.Parameter(typeof(object?[]), "fieldValues");

    public static Func<object?[], object> CompileFactory(
        InputObjectType inputType,
        ConstructorInfo? constructor = null)
    {
        var fields = TypeMemHelper.RentInputFieldMap();
        BuildFieldMap(inputType, fields);

        constructor ??= GetCompatibleConstructor(inputType.RuntimeType, inputType, fields);

        var instance = constructor is null
            ? Expression.New(inputType.RuntimeType)
            : CreateInstance(fields, constructor, _fieldValues);

        if (fields.Count == 0)
        {
            Expression casted = Expression.Convert(instance, typeof(object));
            return Expression.Lambda<Func<object?[], object>>(casted, _fieldValues).Compile();
        }

        var variable = Expression.Variable(inputType.RuntimeType, "obj");

        var expressions = new List<Expression>();
        expressions.Add(Expression.Assign(variable, instance));
        CompileSetProperties(variable, fields.Values, _fieldValues, expressions);
        expressions.Add(Expression.Convert(variable, typeof(object)));
        Expression body = Expression.Block(new[] { variable }, expressions);

        return Expression.Lambda<Func<object?[], object>>(body, _fieldValues).Compile();
    }

    public static Action<object, object?[]> CompileGetFieldValues(InputObjectType inputType)
    {
        Expression instance = _obj;

        if (inputType.RuntimeType != typeof(object))
        {
            instance = Expression.Convert(instance, inputType.RuntimeType);
        }

        var expressions = new List<Expression>();

        foreach (var field in inputType.Fields.AsSpan())
        {
            var getter = field.Property!.GetGetMethod(true)!;
            Expression fieldValue = Expression.Call(instance, getter);
            expressions.Add(SetFieldValue(field, _fieldValues, fieldValue));
        }

        Expression body = Expression.Block(expressions);

        return Expression.Lambda<Action<object, object?[]>>(body, _obj, _fieldValues).Compile();
    }

    private static Expression CreateInstance(
        Dictionary<string, InputField> fields,
        ConstructorInfo constructor,
        Expression fieldValues)
    {
        return Expression.New(
            constructor,
            CompileAssignParameters(fields, constructor, fieldValues));
    }

    private static Expression[] CompileAssignParameters(
        Dictionary<string, InputField> fields,
        ConstructorInfo constructor,
        Expression fieldValues)
    {
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return Array.Empty<Expression>();
        }

        var expressions = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (fields.TryGetParameter(parameter, out var field))
            {
                fields.Remove(field.Property!.Name);
                var value = GetFieldValue(field, fieldValues);

                if (field.IsOptional)
                {
                    value = CreateOptional(value, field.RuntimeType);
                }

                expressions[i] = Expression.Convert(value, parameter.ParameterType);
            }
            else
            {
                throw new InvalidOperationException("Could not resolver parameter.");
            }
        }

        return expressions;
    }

    private static void CompileSetProperties(
        Expression instance,
        IEnumerable<InputField> fields,
        Expression fieldValues,
        List<Expression> currentBlock)
    {
        foreach (var field in fields)
        {
            var setter = field.Property!.GetSetMethod(true)!;
            var value = GetFieldValue(field, fieldValues);

            if (field.IsOptional)
            {
                value = CreateOptional(value, field.RuntimeType);
            }

            value = Expression.Convert(value, field.Property.PropertyType);
            Expression setPropertyValue = Expression.Call(instance, setter, value);
            currentBlock.Add(setPropertyValue);
        }
    }

    private static Expression GetFieldValue(InputField field, Expression fieldValues)
        => Expression.ArrayIndex(fieldValues, Expression.Constant(field.Index));

    private static Expression SetFieldValue(
        InputField field,
        Expression fieldValues,
        Expression fieldValue)
    {
        Expression index = Expression.Constant(field.Index);
        Expression element = Expression.ArrayAccess(fieldValues, index);
        Expression casted = Expression.Convert(fieldValue, typeof(object));
        return Expression.Assign(element, casted);
    }

    private static void BuildFieldMap(InputObjectType type, Dictionary<string, InputField> fields)
    {
        foreach (var field in type.Fields.AsSpan())
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
        return Expression.Call(from!, fieldValue);
    }
}
