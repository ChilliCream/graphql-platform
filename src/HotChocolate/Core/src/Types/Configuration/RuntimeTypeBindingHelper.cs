using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Internal;

namespace HotChocolate.Configuration;

internal static class RuntimeTypeBindingHelper
{
    public static bool RequiresExactBinding(IExtendedType runtimeType)
    {
        ArgumentNullException.ThrowIfNull(runtimeType);

        return IsByteArray(runtimeType) || IsDictionary(runtimeType.Source);
    }

    private static bool IsByteArray(IExtendedType runtimeType)
        => runtimeType.IsArray
            && runtimeType.ElementType is { Source: { } elementType }
            && elementType == typeof(byte);

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2070",
        Justification =
            "The type's interfaces are preserved because the type is part of the schema type system.")]
    private static bool IsDictionary(Type type)
    {
        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.IsGenericType)
        {
            var typeDefinition = type.GetGenericTypeDefinition();

            if (typeDefinition == typeof(IDictionary<,>)
                || typeDefinition == typeof(IReadOnlyDictionary<,>))
            {
                return true;
            }
        }

        foreach (var implementedType in type.GetInterfaces())
        {
            if (implementedType.IsGenericType)
            {
                var typeDefinition = implementedType.GetGenericTypeDefinition();

                if (typeDefinition == typeof(IDictionary<,>)
                    || typeDefinition == typeof(IReadOnlyDictionary<,>))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
