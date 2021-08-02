using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal static class InputObjectConstructorResolver
    {
        public static ConstructorInfo? GetCompatibleConstructor(
            Type type,
            IReadOnlyDictionary<string, InputField> properties)
        {
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo? defaultConstructor = constructors.FirstOrDefault(
                t => t.GetParameters().Length == 0);

            if (properties.Values.All(t => t.Property.CanWrite))
            {
                if (defaultConstructor is not null)
                {
                    return defaultConstructor;
                }

                if (constructors.Length == 0)
                {
                    return null;
                }
            }

            var required = new HashSet<string>();

            foreach (ConstructorInfo constructor in
                constructors.OrderByDescending(t => t.GetParameters().Length))
            {
                if (IsCompatibleConstructor(constructor, properties, required))
                {
                    return constructor;
                }
            }

            throw new InvalidOperationException(
                $"No compatible constructor found for input type type `{type.FullName}`.\r\n" +
                "Either you have to provide a public constructor with settable properties or " +
                "a public constructor that allows to pass in values for read-only properties." +
                $"There was no way to set the following properties: {string.Join(", ", required)}.");
        }

        private static bool IsCompatibleConstructor(
            ConstructorInfo constructor,
            IReadOnlyDictionary<string, InputField> fields,
            ISet<string> required)
        {
            CollectReadOnlyProperties(fields, required);
            return IsCompatibleConstructor(
                constructor.GetParameters(),
                fields,
                required);
        }

        private static bool IsCompatibleConstructor(
            ParameterInfo[] parameters,
            IReadOnlyDictionary<string, InputField> fields,
            ISet<string> required)
        {
            foreach (var parameter in parameters)
            {
                if (fields.TryGetValue(parameter.Name!, out InputField? field) &&
                    parameter.ParameterType ==  field.Property.PropertyType)
                {
                    required.Remove(field.Name);
                }
                else
                {
                    return false;
                }
            }

            return required.Count == 0;
        }

        private static void CollectReadOnlyProperties(
            IReadOnlyDictionary<string, InputField> fields,
            ISet<string> required)
        {
            required.Clear();

            foreach (KeyValuePair<string, InputField> item in fields)
            {
                if (!item.Value.Property.CanWrite)
                {
                    required.Add(item.Key);
                }
            }
        }
    }
}
