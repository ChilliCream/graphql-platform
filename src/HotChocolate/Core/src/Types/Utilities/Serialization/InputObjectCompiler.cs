using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using static HotChocolate.Utilities.Serialization.InputObjectConstructorResolver;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal static class InputObjectCompiler
    {
        public static Func<object?[], object> CompileFactory(
            InputObjectType inputType,
            ConstructorInfo? constructor = null)
        {
            Dictionary<string, InputField> fields = CreateFieldMap(inputType);
            constructor ??= GetCompatibleConstructor(inputType.RuntimeType, fields);

            ParameterExpression fieldValues = Expression.Parameter(typeof(object?[]));
            ParameterExpression variable = Expression.Variable(inputType.RuntimeType, "obj");

            Expression instance = constructor is null
                ? Expression.New(inputType.RuntimeType)
                : CreateInstance(fields, constructor, fieldValues);

            if (fields.Count == 0)
            {
                Expression casted = Expression.Convert(instance, typeof(object));
                return Expression.Lambda<Func<object?[], object>>(casted, fieldValues).Compile();
            }

            var expressions = new List<Expression>();
            expressions.Add(Expression.Assign(variable, instance));
            CompileSetProperties(variable, fields.Values, fieldValues, expressions);
            expressions.Add(Expression.Convert(variable, typeof(object)));
            Expression body = Expression.Block(new[] { variable }, expressions);

            return Expression.Lambda<Func<object?[], object>>(body, fieldValues).Compile();
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
            ParameterInfo[] parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                return Array.Empty<Expression>();
            }

            var expressions = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                InputField field = fields[parameter.Name!];
                fields.Remove(parameter.Name!);
                Expression value = GetFieldValue(field, fieldValues);
                expressions[i] = Expression.Convert(value, parameter.ParameterType);
            }

            return expressions;
        }

        private static void CompileSetProperties(
            Expression instance,
            IEnumerable<InputField> fields,
            Expression fieldValues,
            List<Expression> currentBlock)
        {
            foreach (InputField field in fields)
            {
                MethodInfo setter = field.Property!.GetSetMethod(true)!;
                Expression value = GetFieldValue(field, fieldValues);
                value = Expression.Convert(value, field.Property.PropertyType);
                Expression setPropertyValue = Expression.Call(instance, setter, value);
                currentBlock.Add(setPropertyValue);
            }
        }

        private static Expression GetFieldValue(InputField field, Expression fieldValues)
            => Expression.ArrayIndex(fieldValues, Expression.Constant(field.Index));

        private static Dictionary<string, InputField> CreateFieldMap(InputObjectType type)
            => type.Fields.ToDictionary(t => t.Property!.Name, StringComparer.OrdinalIgnoreCase);
    }
}
