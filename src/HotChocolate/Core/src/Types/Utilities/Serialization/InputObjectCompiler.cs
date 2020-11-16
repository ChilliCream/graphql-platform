using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal static class InputObjectCompiler
    {
        private static readonly MethodInfo _createOptionalValue =
            typeof(InputObjectCompiler).GetMethod(
                nameof(CreateOptionalValue),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo _createValue =
            typeof(InputObjectCompiler).GetMethod(
                nameof(CreateValue),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public static InputObjectFactory CompileFactory(
            InputObjectType inputType,
            ConstructorInfo? constructor)
        {
            ParameterExpression data =
                Expression.Parameter(typeof(IReadOnlyDictionary<string, object>));
            ParameterExpression converter =
                Expression.Parameter(typeof(ITypeConverter));

            ParameterExpression variable = Expression.Variable(inputType.RuntimeType, "obj");

            Expression instance = constructor is null
                ? Expression.New(inputType.RuntimeType)
                : CreateInstance(inputType, constructor, data, converter);

            ParameterInfo[] parameters = constructor is null
                ? Array.Empty<ParameterInfo>()
                : constructor.GetParameters();

            var expressions = new List<Expression>();
            expressions.Add(Expression.Assign(variable, instance));
            expressions.AddRange(SetFields(inputType, parameters, variable, data, converter));
            expressions.Add(Expression.Convert(variable, typeof(object)));

            Expression body = Expression.Block(
                new[] { variable },
                expressions);

            return Expression.Lambda<InputObjectFactory>(body, data, converter).Compile();
        }

        private static Expression CreateInstance(
            InputObjectType inputType,
            ConstructorInfo constructor,
            Expression data,
            Expression converter)
        {
            return Expression.New(
                constructor,
                CreateParameters(inputType, constructor, data, converter));
        }

        private static IEnumerable<Expression> CreateParameters(
            InputObjectType inputType,
            ConstructorInfo constructor,
            Expression data,
            Expression converter)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                yield break;
            }

            Dictionary<string, InputField> fields = inputType.Fields.ToDictionary(t =>
                t.Property!.Name,
                StringComparer.OrdinalIgnoreCase);

            IReadOnlyDictionary<ParameterInfo, InputField> parameterMap =
                CreateParameterMap(parameters, fields);

            foreach (ParameterInfo parameter in parameters)
            {
                yield return GetFieldValue(parameterMap[parameter], data, converter);
            }
        }

        private static IReadOnlyDictionary<ParameterInfo, InputField> CreateParameterMap(
            ParameterInfo[] parameters,
            IReadOnlyDictionary<string, InputField> fields)
        {
            var map = new Dictionary<ParameterInfo, InputField>();

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                map[parameter] = fields[parameter.Name!];
            }

            return map;
        }

        private static IEnumerable<Expression> SetFields(
            InputObjectType inputType,
            IEnumerable<ParameterInfo> parameters,
            Expression instance,
            Expression data,
            Expression converter)
        {
            Dictionary<string, InputField> fields = inputType.Fields.ToDictionary(t =>
                t.Property!.Name,
                StringComparer.OrdinalIgnoreCase);

            foreach (ParameterInfo parameter in parameters)
            {
                fields.Remove(parameter.Name!);
            }

            foreach (InputField field in fields.Values)
            {
                Expression value = GetFieldValue(field, data, converter);
                yield return Expression.Call(instance, field.Property!.GetSetMethod(true)!, value);
            }
        }

        private static Expression GetFieldValue(
            InputField field,
            Expression data,
            Expression converter)
        {
            Type fieldType = field.Property!.PropertyType;
            Expression name = Expression.Constant(field.Name.Value);

            if (fieldType.IsGenericType
                && fieldType.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                MethodInfo createValue = _createOptionalValue.MakeGenericMethod(
                    fieldType.GetGenericArguments());
                return Expression.Call(createValue, data, name, converter);
            }
            else
            {
                MethodInfo createValue = _createValue.MakeGenericMethod(fieldType);
                return Expression.Call(createValue, data, name, converter);
            }
        }

        private static Optional<T> CreateOptionalValue<T>(
            IReadOnlyDictionary<string, object> values,
            string fieldName,
            ITypeConverter converter)
        {
            if (values.TryGetValue(fieldName, out object? o))
            {
                return o is T casted ? casted : converter.Convert<object, T>(o);
            }
            return default;
        }

        private static T CreateValue<T>(
            IReadOnlyDictionary<string, object> values,
            string fieldName,
            ITypeConverter converter)
        {
            if (values.TryGetValue(fieldName, out object? o))
            {
                return o is T casted ? casted : converter.Convert<object, T>(o);
            }
            return default!;
        }
    }
}
