using Mocha.Sagas;

namespace Mocha;

internal static class TypesExtensions
{
    public static bool IsEventRequest(this Type type)
        => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventRequest<>));
}
