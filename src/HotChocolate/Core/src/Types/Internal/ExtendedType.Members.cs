using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        private static class Members
        {
            public static ExtendedType FromMember(MemberInfo member, TypeCache cache) =>
                cache.GetOrCreateType(member, () => Rewrite(FromMember(member), member, cache));

            private static ExtendedType FromMember(MemberInfo member)
            {
                var helper = new NullableHelper(member.DeclaringType!);
                bool? context = helper.GetContext(member);
                ReadOnlySpan<bool?> flags = helper.GetFlags(member);

                return member switch
                {
                    PropertyInfo p => CreateExtendedType(context, flags, p.PropertyType),
                    MethodInfo m => CreateExtendedType(context, flags, m.ReturnType),
                    _ => throw new NotSupportedException(
                        "Only PropertyInfo and MethodInfo are supported.")
                };
            }

            public static ExtendedMethodInfo FromMethod(MethodInfo method, TypeCache cache)
            {
                var helper = new NullableHelper(method.DeclaringType!);
                bool? context = helper.GetContext(method);

                IExtendedType returnType = cache.GetOrCreateType(
                    method, 
                    () => CreateExtendedType(context, helper.GetFlags(method), method.ReturnType));

                ParameterInfo[] parameters = method.GetParameters();
                var parameterTypes = new Dictionary<ParameterInfo, IExtendedType>();

                foreach (ParameterInfo parameter in parameters)
                {
                    parameterTypes.Add(
                        parameter,
                        cache.GetOrCreateType(
                            parameter,
                            () => CreateExtendedType(
                                context,
                                helper.GetFlags(parameter),
                                parameter.ParameterType)));
                }

                return new ExtendedMethodInfo(returnType, parameterTypes);
            }

            private static ExtendedType Rewrite(
                IExtendedType extendedType,
                object? member,
                TypeCache cache)
            {
                extendedType = Helper.RemoveNonEssentialTypes(extendedType);

                IReadOnlyList<IExtendedType> arguments = extendedType.TypeArguments;
                var extendedArguments = new ExtendedType[arguments.Count];

                for (var i = 0; i < extendedArguments.Length; i++)
                {
                    extendedArguments[i] = Rewrite(arguments[i], null, cache);
                }

                ExtendedType? elementType = null;
                var isList = !extendedType.IsArray && Helper.IsSupportedCollectionInterface(extendedType.Type);

                if (isList && extendedType.TypeArguments.Count == 1)
                {
                    Type a = Helper.GetInnerListType(extendedType.Type)!;
                    IExtendedType b = extendedType.TypeArguments[0];
                    if (a == b.Type || a == b.Source)
                    {
                        elementType = extendedArguments[0];
                    }
                }

                if (extendedType.IsArray && elementType is null)
                {
                    elementType = Rewrite(extendedType.ElementType!, null, cache);
                }

                return new ExtendedType(
                    extendedType.Type,
                    ExtendedTypeKind.Runtime,
                    typeArguments: extendedArguments,
                    source: extendedType.Source,
                    definition: extendedType.Definition,
                    elementType: elementType,
                    isList: extendedType.IsList,
                    isNullable: extendedType.IsNullable);
            }

            private static ExtendedType CreateExtendedType(
                bool? context,
                ReadOnlySpan<bool?> flags,
                Type type)
            {
                var position = 0;
                return CreateExtendedType(context, flags, type, ref position);
            }

            private static ExtendedType CreateExtendedType(
                bool? context,
                ReadOnlySpan<bool?> flags,
                Type type,
                ref int position)
            {
                if (type.IsValueType)
                {
                    if (type.IsGenericType)
                    {
                        if (type.IsGenericType
                            && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            return new ExtendedType(
                                type.GetGenericArguments()[0],
                                ExtendedTypeKind.Extended,
                                typeArguments:
                                    GetGenericArguments(context, flags, type, ref position),
                                source: type,
                                definition: typeof(Nullable<>),
                                isNullable: true);
                        }
                    }

                    return new ExtendedType(
                        type,
                        ExtendedTypeKind.Extended,
                        typeArguments: GetGenericArguments(context, flags, type, ref position),
                        isNullable: false);
                }

                bool? state = context;
                if (!flags.IsEmpty)
                {
                    if (flags.Length > position)
                    {
                        state = flags[position++];
                    }
                    else if (flags.Length == 1)
                    {
                        state = flags[0];
                    }
                }

                if (type.IsArray)
                {
                    ExtendedType elementType =
                        CreateExtendedType(
                            context,
                            flags,
                            type.GetElementType()!,
                            ref position);

                    return new ExtendedType(
                        type,
                        ExtendedTypeKind.Extended,
                        typeArguments: new[] { elementType },
                        elementType: elementType,
                        source: type,
                        isNullable: state ?? false);
                }

                return new ExtendedType(
                    type,
                    ExtendedTypeKind.Extended,
                    typeArguments: GetGenericArguments(context, flags, type, ref position),
                    source: type,
                    isNullable: state ?? false);
            }

            private static IReadOnlyList<ExtendedType> GetGenericArguments(
                bool? context,
                ReadOnlySpan<bool?> flags,
                Type type,
                ref int position)
            {
                if (type.IsGenericType)
                {
                    Type[] arguments = type.GetGenericArguments();
                    ExtendedType[] extendedArguments = new ExtendedType[arguments.Length];

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        extendedArguments[i] =
                            CreateExtendedType(context, flags, arguments[i], ref position);
                    }

                    return extendedArguments;
                }

                return Array.Empty<ExtendedType>();
            }
        }
    }
}
