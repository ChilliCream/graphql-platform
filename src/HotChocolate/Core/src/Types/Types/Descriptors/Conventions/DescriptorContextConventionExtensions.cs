#nullable enable

namespace HotChocolate.Types.Descriptors;

public static class DescriptorContextConventionExtensions
{
    public static T GetConventionOrDefault<T>(
        this IDescriptorContext context,
        T defaultConvention)
        where T : class, IConvention =>
        context.GetConventionOrDefault(() => defaultConvention);

    public static T GetConventionOrDefault<T>(
        this IDescriptorContext context,
        Func<T> defaultConvention)
        where T : class, IConvention =>
        context.GetConventionOrDefault(defaultConvention);

    public static T GetConventionOrDefault<T>(
        this IDescriptorContext context,
        string? scope,
        T defaultConvention)
        where T : class, IConvention =>
        context.GetConventionOrDefault(() => defaultConvention, scope);
}
