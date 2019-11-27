using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable enable

namespace HotChocolate.Utilities
{
    public class NullableHelper
    {
        private readonly Type _type;
        private Nullable _context = Nullable.Yes;

        public NullableHelper(Type type)
        {
            _type = type;

            _context = GetContext(
                type.Assembly.GetCustomAttribute<NullableContextAttribute>());

            _context = GetContext(
                type.GetCustomAttribute<NullableContextAttribute>());
        }

        public Type Type => _type;

        public TypeNullability GetPropertyInfo(PropertyInfo property)
        {
            return CreateNullableTypeInfo(
                GetContext(property),
                GetFlags(property),
                property.PropertyType);
        }

        public MethodTypeInfo GetMethodInfo(MethodInfo method)
        {
            TypeNullability returnType = CreateNullableTypeInfo(
                GetContext(method),
                GetFlags(method),
                method.ReturnType);

            ParameterInfo[] parameters = method.GetParameters();
            TypeNullability[] parameterTypes = new TypeNullability[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                parameterTypes[i] = CreateNullableTypeInfo(
                    GetContext(parameter),
                    GetFlags(parameter),
                    parameter.ParameterType);
            }

            return new MethodTypeInfo(returnType, parameterTypes);
        }

        private TypeNullability CreateNullableTypeInfo(
            Nullable context,
            ReadOnlySpan<byte> flags,
            Type type)
        {
            int position = 0;
            return CreateNullableTypeInfo(context, flags, type, ref position);
        }

        private TypeNullability CreateNullableTypeInfo(
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
                        return new TypeNullability(Nullable.Yes, type);
                    }
                    else
                    {
                        var arguments = new List<TypeNullability>();
                        foreach (Type argumentType in type.GetGenericArguments())
                        {
                            arguments.Add(CreateNullableTypeInfo(
                                context, flags, argumentType, ref position));
                        }
                        return new TypeNullability(Nullable.No, type, arguments);
                    }
                }
                else
                {
                    return new TypeNullability(Nullable.No, type);
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
                    var arguments = new List<TypeNullability>();
                    foreach (Type argumentType in type.GetGenericArguments())
                    {
                        arguments.Add(CreateNullableTypeInfo(
                            context, flags, argumentType, ref position));
                    }
                    return new TypeNullability(state, type, arguments);
                }
                else
                {
                    return new TypeNullability(state, type);
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

        private Nullable GetContext(
            NullableContextAttribute? attribute,
            Nullable parent)
        {
            if (attribute is { })
            {
                return (Nullable)attribute.Flag;
            }
            return parent;
        }

        private ReadOnlySpan<byte> GetFlags(MemberInfo member)
        {
            return GetFlags(member.GetCustomAttribute<NullableAttribute>());
        }

        private ReadOnlySpan<byte> GetFlags(ParameterInfo parameter)
        {
            return GetFlags(parameter.GetCustomAttribute<NullableAttribute>());
        }

        private ReadOnlySpan<byte> GetFlags(NullableAttribute? attribute)
        {
            if (attribute is { })
            {
                return attribute.Flags;
            }
            return default;
        }
    }

    public enum Nullable : byte
    {
        Yes = 2,
        No = 1
    }

    public class MethodTypeInfo
    {
        public MethodTypeInfo(
            TypeNullability returnType,
            IReadOnlyList<TypeNullability> parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public TypeNullability ReturnType { get; }

        public IReadOnlyList<TypeNullability> ParameterTypes { get; }
    }

    public class TypeNullability
    {
        public TypeNullability(Nullable state, Type type)
            : this(state, type, Array.Empty<TypeNullability>())
        {
        }

        public TypeNullability(
            Nullable state,
            Type type,
            IReadOnlyList<TypeNullability> genericArguments)
        {
            State = state;
            Type = type;
            GenericArguments = genericArguments;
        }

        public Nullable State { get; }

        public Type Type { get; }

        public IReadOnlyList<TypeNullability> GenericArguments { get; }
    }
}
