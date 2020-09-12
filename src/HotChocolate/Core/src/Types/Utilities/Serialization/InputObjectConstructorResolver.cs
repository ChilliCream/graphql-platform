using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#nullable enable

namespace HotChocolate.Utilities.Serialization
{
    internal static class InputObjectConstructorResolver
    {
        public static ConstructorInfo? GetConstructor(
            Type type,
            IEnumerable<PropertyInfo> properties)
        {
            Dictionary<string, PropertyInfo> propertyMap = properties.ToDictionary(
                t => t.Name,
                StringComparer.OrdinalIgnoreCase);
            return GetCompatibleConstructor(type, propertyMap);
        }

        private static ConstructorInfo? GetCompatibleConstructor(
            Type type,
            IReadOnlyDictionary<string, PropertyInfo> properties)
        {
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo? defaultConstructor = constructors.FirstOrDefault(
                t => t.GetParameters().Length == 0);

            if (properties.Values.All(t => t.CanWrite))
            {
                if (defaultConstructor is not null)
                {
                    return defaultConstructor;
                }
                else if (constructors.Length == 0)
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
            IReadOnlyDictionary<string, PropertyInfo> properties,
            ISet<string> required)
        {
            CollectReadOnlyProperties(properties, required);
            return IsCompatibleConstructor(
                constructor.GetParameters(),
                properties,
                required);
        }

        private static bool IsCompatibleConstructor(
            ParameterInfo[] parameters,
            IReadOnlyDictionary<string, PropertyInfo> properties,
            ISet<string> required)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (properties.TryGetValue(parameter.Name!, out PropertyInfo? property)
                    && parameter.ParameterType == property.PropertyType)
                {
                    required.Remove(property.Name);
                }
                else
                {
                    return false;
                }
            }

            return required.Count == 0;
        }

        private static void CollectReadOnlyProperties(
            IReadOnlyDictionary<string, PropertyInfo> properties,
            ISet<string> required)
        {
            required.Clear();

            foreach (KeyValuePair<string, PropertyInfo> item in properties)
            {
                if (!item.Value.CanWrite)
                {
                    required.Add(item.Key);
                }
            }
        }
    }
}
