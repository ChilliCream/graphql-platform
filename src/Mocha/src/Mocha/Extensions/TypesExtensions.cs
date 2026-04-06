using System.Diagnostics.CodeAnalysis;

namespace Mocha;

internal static class TypesExtensions
{
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Metadata read on statically-referenced types is AOT-safe.")]
    public static bool IsEventRequest(this Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventRequest<>));
}
