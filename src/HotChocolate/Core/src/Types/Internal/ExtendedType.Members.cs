using System.Reflection;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    private static class Members
    {
        public static ExtendedType FromMember(MemberInfo member, TypeCache cache) =>
            cache.GetOrCreateType(
                member,
                () => Rewrite(FromMember(member), member, cache));

        private static ExtendedType FromMember(MemberInfo member)
        {
            var helper = new NullableHelper(member.DeclaringType!);
            var context = helper.GetContext(member);
            ReadOnlySpan<bool?> flags = helper.GetFlags(member);

            return member switch
            {
                PropertyInfo p => CreateExtendedType(context, flags, p.PropertyType),
                MethodInfo m => CreateExtendedType(context, flags, m.ReturnType),
                _ => throw new NotSupportedException(
                    "Only PropertyInfo and MethodInfo are supported."),
            };
        }

        public static ExtendedMethodInfo FromMethod(MethodInfo method, TypeCache cache)
        {
            var helper = new NullableHelper(method.DeclaringType!);
            var context = helper.GetContext(method);

            IExtendedType returnType = cache.GetOrCreateType(
                method,
                () => Rewrite(
                    CreateExtendedType(context, helper.GetFlags(method), method.ReturnType),
                    method,
                    cache));

            var parameters = method.GetParameters();
            var parameterTypes = new Dictionary<ParameterInfo, IExtendedType>();

            foreach (var parameter in parameters)
            {
                parameterTypes.Add(
                    parameter,
                    cache.GetOrCreateType(
                        parameter,
                        () => Rewrite(
                            CreateExtendedType(
                                context,
                                helper.GetFlags(parameter),
                                parameter.ParameterType),
                            parameter,
                            cache)));
            }

            return new ExtendedMethodInfo(returnType, parameterTypes);
        }

        private static ExtendedType Rewrite(
            IExtendedType extendedType,
            object? member,
            TypeCache cache)
        {
            extendedType = Helper.RemoveNonEssentialTypes(extendedType);

            var arguments = extendedType.TypeArguments;
            var extendedArguments = new ExtendedType[arguments.Count];

            for (var i = 0; i < extendedArguments.Length; i++)
            {
                extendedArguments[i] = Rewrite(arguments[i], null, cache);
            }

            ExtendedType? elementType = null;
            var isList =
                !extendedType.IsArray &&
                Helper.IsListType(extendedType.Type);

            if (isList)
            {
                var itemType = Helper.GetInnerListType(extendedType.Type)!;

                if (extendedType.TypeArguments.Count == 1)
                {
                    var typeArgument = extendedType.TypeArguments[0];
                    if (itemType == typeArgument.Type || itemType == typeArgument.Source)
                    {
                        elementType = extendedArguments[0];
                    }
                }

                if (elementType is null)
                {
                    elementType = ExtendedType.FromType(itemType, cache);
                }
            }

            if (extendedType.IsArray && elementType is null)
            {
                elementType = Rewrite(extendedType.ElementType!, null, cache);
            }

            var rewritten = new ExtendedType(
                extendedType.Type,
                ExtendedTypeKind.Runtime,
                typeArguments: extendedArguments,
                source: extendedType.Source,
                definition: extendedType.Definition,
                elementType: elementType,
                isList: isList,
                isNullable: extendedType.IsNullable);

            return cache.TryAdd(rewritten, member)
                ? rewritten
                : cache.GetType(rewritten.Id);
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
            var state = position == -1 || (type.IsValueType && !type.IsGenericType)
                ? null
                : GetNextState(flags, ref position);

            if (type.IsValueType)
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var inner = type.GetGenericArguments()[0];

                    return new ExtendedType(
                        inner,
                        ExtendedTypeKind.Extended,
                        typeArguments: GetGenericArguments(context, flags, inner, ref position),
                        source: type,
                        isNullable: true);
                }

                return new ExtendedType(
                    type,
                    ExtendedTypeKind.Extended,
                    typeArguments: GetGenericArguments(context, flags, type, ref position),
                    source: type,
                    isNullable: false);
            }

            if (type.IsArray)
            {
                var elementType =
                    CreateExtendedType(
                        context,
                        flags,
                        type.GetElementType()!,
                        ref position);

                return new ExtendedType(
                    type,
                    ExtendedTypeKind.Extended,
                    typeArguments: new[] { elementType, },
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

            bool? GetNextState(ReadOnlySpan<bool?> flags, ref int position)
            {
                var state = context;
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

                return state;
            }
        }

        private static IReadOnlyList<ExtendedType> GetGenericArguments(
            bool? context,
            ReadOnlySpan<bool?> flags,
            Type type,
            ref int position)
        {
            if (type.IsGenericType)
            {
                var arguments = type.GetGenericArguments();
                var extendedArguments = new ExtendedType[arguments.Length];
                var skipFlags = SkipFlags(arguments);
                var skipPos = -1;

                for (var i = 0; i < arguments.Length; i++)
                {
                    extendedArguments[i] =
                        skipFlags
                            ? CreateExtendedType(context, flags, arguments[i], ref skipPos)
                            : CreateExtendedType(context, flags, arguments[i], ref position);
                }

                return extendedArguments;
            }

            return Array.Empty<ExtendedType>();
        }

        private static bool SkipFlags(Type[] arguments)
        {
            var skipFlags = true;

            foreach (var argument in arguments)
            {
                if (!argument.IsValueType)
                {
                    skipFlags = false;
                }
                else if (argument.IsGenericType)
                {
                    if (argument.GetGenericTypeDefinition() != typeof(Nullable<>))
                    {
                        skipFlags = false;
                    }
                    else
                    {
                        skipFlags = SkipFlags(argument.GetGenericArguments());
                    }
                }
            }

            return skipFlags;
        }
    }
}
