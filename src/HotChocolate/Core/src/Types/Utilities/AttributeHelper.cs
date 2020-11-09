using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

#nullable  enable

namespace HotChocolate.Utilities
{
    internal static class AttributeHelper
    {
        private const string _clone = "<Clone>$";

        public static bool TryGetAttribute<T>(
            ICustomAttributeProvider attributeProvider,
            [NotNullWhen(true)] out T? attribute)
            where T : Attribute
        {
            if (attributeProvider is PropertyInfo p &&
                p.DeclaringType is not null &&
                IsRecord(p.DeclaringType))
            {
                if (IsDefinedOnRecord<T>(p, true))
                {
                    attribute = GetCustomAttributeFromRecord<T>(p, true)!;
                    return true;
                }
            }
            else if (attributeProvider.IsDefined(typeof(T), true))
            {
                attribute = attributeProvider
                    .GetCustomAttributes(typeof(T), true)
                    .OfType<T>()
                    .First();
                return true;
            }

            attribute = null;
            return false;
        }

        private static bool IsRecord(Type type)
        {
            return IsRecord(type.GetMembers());
        }

        private static bool IsRecord(IReadOnlyList<MemberInfo> members)
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (IsCloneMember(members[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCloneMember(MemberInfo member) =>
            EqualsOrdinal(member.Name, _clone);

        public static IEnumerable<T> GetCustomAttributes<T>(
            ICustomAttributeProvider attributeProvider,
            bool inherit)
            where T : Attribute
        {
            if (attributeProvider is PropertyInfo p &&
                p.DeclaringType is not null &&
                IsRecord(p.DeclaringType))
            {
                return GetCustomAttributesFromRecord<T>(p, inherit);
            }

            return attributeProvider.GetCustomAttributes(true).OfType<T>();
        }

        private static IEnumerable<T> GetCustomAttributesFromRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            IEnumerable<T> attributes = Enumerable.Empty<T>();

            if (property.IsDefined(typeof(T)))
            {
                attributes = attributes.Concat(property.GetCustomAttributes<T>(inherit));
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (EqualsOrdinal(parameter.Name, property.Name))
                    {
                        attributes = attributes.Concat(parameter.GetCustomAttributes<T>(inherit));
                    }
                }
            }

            return attributes;
        }

        private static T? GetCustomAttributeFromRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            if (property.IsDefined(typeof(T)))
            {
                return property.GetCustomAttribute<T>(inherit);
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (EqualsOrdinal(parameter.Name, property.Name))
                    {
                        return parameter.GetCustomAttribute<T>(inherit);
                    }
                }
            }

            return null;
        }

        private static bool IsDefinedOnRecord<T>(
            PropertyInfo property,
            bool inherit)
            where T : Attribute
        {
            Type recordType = property.DeclaringType!;
            ConstructorInfo[] constructors = recordType.GetConstructors();

            if (property.IsDefined(typeof(T), inherit))
            {
                return true;
            }

            if (constructors.Length == 1)
            {
                foreach (ParameterInfo parameter in constructors[0].GetParameters())
                {
                    if (EqualsOrdinal(parameter.Name, property.Name))
                    {
                        return parameter.IsDefined(typeof(T));
                    }
                }
            }

            return false;
        }

        private static bool EqualsOrdinal(this string? s, string? other) =>
            string.Equals(s, other, StringComparison.Ordinal);
    }
}
