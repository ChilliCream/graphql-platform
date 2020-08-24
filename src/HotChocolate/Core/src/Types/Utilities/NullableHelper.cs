using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities.CompilerServices;

#nullable enable

namespace HotChocolate.Utilities
{
    internal readonly ref struct NullableHelper
    {
        private const string _nullableContextAttributeName =
            "System.Runtime.CompilerServices.NullableContextAttribute";
        private const string _nullableAttributeName =
            "System.Runtime.CompilerServices.NullableAttribute";

        private readonly Nullable _context;

        public NullableHelper(Type type)
        {
            _context = GetContext(GetNullableContextAttribute(type.Assembly), Nullable.Yes);

            Type? current = type.DeclaringType;
            while (current != null)
            {
                _context = GetContext(GetNullableContextAttribute(current), _context);
                current = current.DeclaringType;
            }

            _context = GetContext(GetNullableContextAttribute(type), _context);
        }

        public static IExtendedType GetReturnType(MemberInfo member)
        {
            return ExtendedType.FromExtendedType(GetReturnTypeInternal(member), member);
        }

        private static IExtendedType GetReturnTypeInternal(MemberInfo member)
        {
            var helper = new NullableHelper(member.DeclaringType!);
            Nullable context = helper.GetContext(member);
            ReadOnlySpan<byte> flags = GetFlags(member);

            switch (member)
            {
                case PropertyInfo p:
                    return helper.CreateExtendedType(context, flags, p.PropertyType);

                case MethodInfo m:
                    return helper.CreateExtendedType(context, flags, m.ReturnType);

                default:
                    throw new NotSupportedException(
                        "Only PropertyInfo and MethodInfo are supported.");
            }
        }

        public static ExtendedMethodTypeInfo GetExtendedMethodInfo(MethodInfo method)
        {
            var helper = new NullableHelper(method.DeclaringType!);
            return helper.GetMethodInfo(method);
        }

        private ExtendedMethodTypeInfo GetMethodInfo(MethodInfo method)
        {
            IExtendedType returnType = ExtendedType.FromExtendedType(
                CreateExtendedType(GetContext(method), GetFlags(method), method.ReturnType),
                method);

            ParameterInfo[] parameters = method.GetParameters();
            var parameterTypes = new Dictionary<ParameterInfo, IExtendedType>();

            foreach (ParameterInfo parameter in parameters)
            {
                parameterTypes.Add(
                    parameter,
                    ExtendedType.FromExtendedType(
                        CreateExtendedType(
                            GetContext(parameter),
                            GetFlags(parameter),
                            parameter.ParameterType),
                        parameter));
            }

            return new ExtendedMethodTypeInfo(returnType, parameterTypes);
        }

        private IExtendedType CreateExtendedType(
            Nullable context,
            ReadOnlySpan<byte> flags,
            Type type)
        {
            var position = 0;
            return CreateExtendedType(context, flags, type, ref position);
        }

        private IExtendedType CreateExtendedType(
            Nullable context,
            ReadOnlySpan<byte> flags,
            Type type,
            ref int position)
        {
            if (type.IsValueType)
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return new Internal.ExtendedType(
                            type.GetGenericArguments()[0],
                            true,
                            ExtendedTypeKind.Extended);
                    }

                    var arguments = new List<IExtendedType>();
                    foreach (Type argumentType in type.GetGenericArguments())
                    {
                        arguments.Add(CreateExtendedType(
                            context, flags, argumentType, ref position));
                    }
                    return new Internal.ExtendedType(
                        type,
                        false,
                        ExtendedTypeKind.Extended,
                        typeArguments: arguments);
                }

                return new Internal.ExtendedType(
                    type,
                    false,
                    ExtendedTypeKind.Extended);
            }

            Nullable state = context;
            if (!flags.IsEmpty)
            {
                if (flags.Length > position)
                {
                    state = (Nullable)flags[position++];
                }
                else if (flags.Length == 1)
                {
                    state = (Nullable)flags[0];
                }
            }

            if (type.IsGenericType)
            {
                var arguments = new List<IExtendedType>();

                foreach (Type argumentType in type.GetGenericArguments())
                {
                    arguments.Add(CreateExtendedType(
                        context, flags, argumentType, ref position));
                }

                return new Internal.ExtendedType(
                    type,
                    state == Nullable.Yes,
                    ExtendedTypeKind.Extended,
                    typeArguments: arguments);
            }

            if (type.IsArray)
            {
                IExtendedType elementType =
                    CreateExtendedType(
                        context,
                        flags,
                        type.GetElementType()!,
                        ref position);

                return new Internal.ExtendedType(
                    type,
                    state == Nullable.Yes,
                    ExtendedTypeKind.Extended,
                    typeArguments: new[] { elementType },
                    elementType: elementType);
            }

            return new Internal.ExtendedType(
                type,
                state == Nullable.Yes,
                ExtendedTypeKind.Extended);
        }

        private Nullable GetContext(MemberInfo member)
        {
            NullableContextAttribute? attribute = GetNullableContextAttribute(member);
            return GetContext(attribute);
        }

        private Nullable GetContext(ParameterInfo parameter)
        {
            NullableContextAttribute? attribute = GetNullableContextAttribute(parameter);
            return GetContext(attribute, GetContext(parameter.Member));
        }

        private Nullable GetContext(NullableContextAttribute? attribute)
        {
            return GetContext(attribute, _context);
        }

        private static Nullable GetContext(
            NullableContextAttribute? attribute,
            Nullable parent)
        {
            if (attribute is { })
            {
                return (Nullable)attribute.Flag;
            }
            return parent;
        }

        private static ReadOnlySpan<byte> GetFlags(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                return GetFlags(GetNullableAttribute(m));
            }
            return GetFlags(GetNullableAttribute(member));
        }

        private static ReadOnlySpan<byte> GetFlags(ParameterInfo parameter)
        {
            return GetFlags(GetNullableAttribute(parameter));
        }

        private static ReadOnlySpan<byte> GetFlags(NullableAttribute? attribute)
        {
            if (attribute is { })
            {
                return attribute.Flags;
            }
            return default;
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

            if (data is { })
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

            if (data is { })
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
                        throw new InvalidOperationException(
                            "Unexpected nullable attribute data.");
                }
            }

            return null;
        }
    }
}
