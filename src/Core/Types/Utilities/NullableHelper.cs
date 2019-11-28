using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Utilities
{
    internal readonly ref struct NullableHelper
    {
        private readonly Type _type;
        private readonly Nullable _context;

        public NullableHelper(Type type)
        {
            _type = type;

            _context = GetContext(
                type.Assembly.GetCustomAttribute<NullableContextAttribute>(),
                Nullable.Yes);

            _context = GetContext(
                type.GetCustomAttribute<NullableContextAttribute>(),
                _context);
        }

        public Type Type => _type;

        public IExtendedType GetPropertyInfo(PropertyInfo property)
        {
            return CreateNullableTypeInfo(
                GetContext(property),
                GetFlags(property),
                property.PropertyType);
        }

        public ExtendedMethodTypeInfo GetMethodInfo(MethodInfo method)
        {
            IExtendedType returnType = CreateNullableTypeInfo(
                GetContext(method),
                GetFlags(method),
                method.ReturnType);

            ParameterInfo[] parameters = method.GetParameters();
            IExtendedType[] parameterTypes = new IExtendedType[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                parameterTypes[i] = CreateNullableTypeInfo(
                    GetContext(parameter),
                    GetFlags(parameter),
                    parameter.ParameterType);
            }

            return new ExtendedMethodTypeInfo(returnType, parameterTypes);
        }

        private IExtendedType CreateNullableTypeInfo(
            Nullable context,
            ReadOnlySpan<byte> flags,
            Type type)
        {
            int position = 0;
            return CreateNullableTypeInfo(context, flags, type, ref position);
        }

        private IExtendedType CreateNullableTypeInfo(
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
                        return new ExtendedType(
                            type,
                            true,
                            ExtendedTypeKind.Extended);
                    }
                    else
                    {
                        var arguments = new List<IExtendedType>();
                        foreach (Type argumentType in type.GetGenericArguments())
                        {
                            arguments.Add(CreateNullableTypeInfo(
                                context, flags, argumentType, ref position));
                        }
                        return new ExtendedType(
                            type,
                            false,
                            ExtendedTypeKind.Extended,
                            arguments);
                    }
                }
                else
                {
                    return new ExtendedType(
                        type,
                        false,
                        ExtendedTypeKind.Extended);
                }
            }
            else
            {
                Nullable state = context;
                if (!flags.IsEmpty && flags.Length > position)
                {
                    state = (Nullable)flags[position++];
                }

                if (type.IsGenericType)
                {
                    var arguments = new List<IExtendedType>();
                    foreach (Type argumentType in type.GetGenericArguments())
                    {
                        arguments.Add(CreateNullableTypeInfo(
                            context, flags, argumentType, ref position));
                    }
                    return new ExtendedType(
                        type,
                        state == Nullable.Yes,
                        ExtendedTypeKind.Extended,
                        arguments);
                }
                else
                {
                    return new ExtendedType(
                        type,
                        state == Nullable.Yes,
                        ExtendedTypeKind.Extended);
                }
            }
        }

        private Nullable GetContext(MemberInfo member)
        {
            NullableContextAttribute? attribute =
                member.GetCustomAttribute<NullableContextAttribute>();
            return GetContext(attribute);
        }

        private Nullable GetContext(ParameterInfo parameter)
        {
            NullableContextAttribute? attribute =
                parameter.GetCustomAttribute<NullableContextAttribute>();
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
            return GetFlags(member.GetCustomAttribute<NullableAttribute>());
        }

        private static ReadOnlySpan<byte> GetFlags(ParameterInfo parameter)
        {
            return GetFlags(parameter.GetCustomAttribute<NullableAttribute>());
        }

        private static ReadOnlySpan<byte> GetFlags(NullableAttribute? attribute)
        {
            if (attribute is { })
            {
                return attribute.Flags;
            }
            return default;
        }
    }
}
