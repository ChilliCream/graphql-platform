using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Types.Tests.Types
{
    public class Foo
    {
        [Fact]
        public void Bar()
        {
            PropertyInfo[] props = typeof(Test).GetProperties();
            Dictionary<string, PropertyInfo> properties = props.ToDictionary(
                t => t.Name,
                StringComparer.OrdinalIgnoreCase);
            ConstructorInfo constructor = GetCompatibleConstructor(typeof(Test), properties);

            Func<object[], object> d;

            d(new[] { "a" });

        }

        private static ConstructorInfo GetCompatibleConstructor(
            Type type,
            IReadOnlyDictionary<string, PropertyInfo> properties)
        {
            ConstructorInfo[] constructors = type.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            ConstructorInfo defaultConstructor = constructors.FirstOrDefault(t =>
                t.GetParameters().Length == 0);

            if (properties.Values.All(t => t.CanWrite))
            {
                return defaultConstructor;
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

            throw new InvalidOperationException();
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
                if (properties.TryGetValue(parameter.Name, out PropertyInfo property)
                    && parameter.ParameterType == property.PropertyType)
                {
                    required.Remove(property.Name);
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

        public class Test
        {
            public Test()
            {

            }

            private Test(string a)
            {

            }

            public string A { get; }

            public string B { get; set; }
        }
    }
}
