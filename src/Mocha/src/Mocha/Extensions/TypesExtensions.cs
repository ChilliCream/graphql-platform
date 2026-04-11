namespace Mocha;

internal static class TypesExtensions
{
    public static bool IsEventRequest(this Type type)
        => typeof(IEventRequest).IsAssignableFrom(type);
}
