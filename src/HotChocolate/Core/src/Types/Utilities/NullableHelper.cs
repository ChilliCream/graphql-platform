using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using HotChocolate.Utilities.CompilerServices;

#nullable enable

namespace HotChocolate.Utilities
{
    internal readonly struct NullableHelper
    {
        private const string _nullableContextAttributeName =
            "System.Runtime.CompilerServices.NullableContextAttribute";
        private const string _nullableAttributeName =
            "System.Runtime.CompilerServices.NullableAttribute";

        private readonly bool? _context;

        public NullableHelper(Type type)
        {
            _context = GetContext(GetNullableContextAttribute(type.Assembly), true);

            Type? current = type.DeclaringType;
            while (current != null)
            {
                _context = GetContext(GetNullableContextAttribute(current), _context);
                current = current.DeclaringType;
            }

            _context = GetContext(GetNullableContextAttribute(type), _context);
        }

        public bool? GetContext(MemberInfo member)
        {
            NullableContextAttribute? attribute = GetNullableContextAttribute(member);
            return GetContext(attribute);
        }

        private bool? GetContext(ParameterInfo parameter)
        {
            NullableContextAttribute? attribute = GetNullableContextAttribute(parameter);
            return GetContext(attribute, GetContext(parameter.Member));
        }

        private bool? GetContext(NullableContextAttribute? attribute)
        {
            return GetContext(attribute, _context);
        }

        private static bool? GetContext(
            NullableContextAttribute? attribute,
            bool? parent)
        {
            if (attribute is not null)
            {
                return (Nullable)attribute.Flag switch
                {
                    Nullable.Yes => true,
                    Nullable.No => false,
                    _ => null
                };
            }
            return parent;
        }

        public bool?[] GetFlags(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                return GetFlags(GetNullableAttribute(m));
            }
            return GetFlags(GetNullableAttribute(member));
        }

        public bool?[] GetFlags(ParameterInfo parameter)
        {
            return GetFlags(GetNullableAttribute(parameter));
        }

        private static bool?[] GetFlags(NullableAttribute? attribute)
        {
            if (attribute is not null)
            {
                var flags = new bool?[attribute.Flags.Length];

                for (var i = 0; i < attribute.Flags.Length; i++)
                {
                    flags[i] = (Nullable)attribute.Flags[i] switch
                    {
                        Nullable.Yes => true,
                        Nullable.No => false,
                        _ => null
                    };
                }

                return flags;
            }
            return Array.Empty<bool?>();
        }

        private static NullableContextAttribute? GetNullableContextAttribute(
            MemberInfo member) =>
            GetNullableContextAttribute(member.GetCustomAttributesData());

        private static NullableContextAttribute? GetNullableContextAttribute(
            ParameterInfo member) =>
            GetNullableContextAttribute(member.GetCustomAttributesData());

        private static NullableContextAttribute? GetNullableContextAttribute(
            Assembly assembly) =>
            GetNullableContextAttribute(assembly.GetCustomAttributesData());

        private static NullableContextAttribute? GetNullableContextAttribute(
            IList<CustomAttributeData> attributes)
        {
            CustomAttributeData? data = attributes.FirstOrDefault(t =>
                t.AttributeType.FullName.EqualsOrdinal(_nullableContextAttributeName));

            if (data is not null)
            {
                return new NullableContextAttribute(
                    (byte)data.ConstructorArguments[0].Value!);
            }

            return null;
        }

        private static NullableAttribute? GetNullableAttribute(
            MethodInfo method)
        {
            object[] attributes = method.ReturnTypeCustomAttributes.GetCustomAttributes(false);
            object? attribute = attributes.FirstOrDefault(t =>
                t.GetType().FullName.EqualsOrdinal(_nullableAttributeName));

            if (attribute is null)
            {
                return GetNullableAttribute((MemberInfo)method);
            }

            try
            {
                var flags = (byte[])attribute.GetType()
                    .GetField("NullableFlags")!
                    .GetValue(attribute)!;
                return new NullableAttribute(flags);
            }
            catch
            {
                return null;
            }
        }

        private static NullableAttribute? GetNullableAttribute(
            MemberInfo member) =>
            GetNullableAttribute(member.GetCustomAttributesData());

        private static NullableAttribute? GetNullableAttribute(
            ParameterInfo parameter) =>
            GetNullableAttribute(parameter.GetCustomAttributesData());

        private static NullableAttribute? GetNullableAttribute(
            IList<CustomAttributeData> attributes)
        {
            CustomAttributeData? data = attributes.FirstOrDefault(t =>
                t.AttributeType.FullName.EqualsOrdinal(_nullableAttributeName));

            if (data is not null)
            {
                switch (data.ConstructorArguments[0].Value)
                {
                    case byte b:
                        return new NullableAttribute(b);
                    case byte[] a:
                        return new NullableAttribute(a);
                    case CustomAttributeTypedArgument b:
                        return new NullableAttribute((byte)b.Value!);
                    case ReadOnlyCollection<CustomAttributeTypedArgument> a:
                        return new NullableAttribute(a.Select(t => (byte)t.Value!).ToArray());
                    default:
                        throw new InvalidOperationException("Unexpected nullable attribute data.");
                }
            }

            return null;
        }
    }
}
