using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    internal static class Tools
    {
        internal static bool IsSchemaType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Helper.IsSchemaType(type);
        }

        internal static bool IsGenericBaseType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return BaseTypes.IsGenericBaseType(type);
        }

        internal static bool IsNonGenericBaseType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return BaseTypes.IsNonGenericBaseType(type);
        }

        internal static Type? GetElementType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Helper.GetInnerListType(type);
        }

        internal static Type? GetNamedType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (BaseTypes.IsNamedType(type))
            {
                return type;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (typeof(ListType<>) == definition
                    || typeof(NonNullType<>) == definition
                    || typeof(NativeType<>) == definition)
                {
                    return GetNamedType(type.GetGenericArguments()[0]);
                }
            }

            return null;
        }

        internal static ExtendedTypeId CreateId(
            IExtendedType type,
            ReadOnlySpan<bool?> nullabilityChange)
        {
            ArgumentNullException.ThrowIfNull(type);

            return nullabilityChange.Length == 0
                ? Helper.CreateIdentifier(type)
                : Helper.CreateIdentifier(type, nullabilityChange);
        }

        internal static IExtendedType ChangeNullability(
            IExtendedType type,
            ReadOnlySpan<bool?> nullable,
            TypeCache cache)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(cache);

            return Helper.ChangeNullability(type, nullable, cache);
        }

        internal static bool?[] CollectNullability(IExtendedType type)
        {
            var length = 0;
            Span<bool> buffer = stackalloc bool[32];
            Helper.CollectNullability(type, buffer, ref length);
            buffer = buffer[..length];

            var nullability = new bool?[buffer.Length];
            for (var i = 0; i < nullability.Length; i++)
            {
                nullability[i] = buffer[i];
            }
            return nullability;
        }

        internal static bool CollectNullability(
            IExtendedType type,
            Span<bool?> nullability,
            out int written)
        {
            var length = 0;
            Span<bool> buffer = stackalloc bool[32];
            Helper.CollectNullability(type, buffer, ref length);
            buffer = buffer[..length];

            if (nullability.Length < buffer.Length)
            {
                written = 0;
                return false;
            }

            for (var i = 0; i < buffer.Length; i++)
            {
                nullability[i] = buffer[i];
            }

            written = buffer.Length;
            return true;
        }
    }
}
