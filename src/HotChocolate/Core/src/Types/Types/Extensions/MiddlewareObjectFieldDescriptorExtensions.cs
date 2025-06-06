using HotChocolate.Resolvers;

namespace HotChocolate.Types;

public static class MiddlewareObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor Use<TMiddleware>(
        this IObjectFieldDescriptor descriptor)
        where TMiddleware : class
    {
        return descriptor.Use(FieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    public static IObjectFieldDescriptor Use<TMiddleware>(
        this IObjectFieldDescriptor descriptor,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return descriptor.Use(FieldClassMiddlewareFactory.Create(factory));
    }
}
