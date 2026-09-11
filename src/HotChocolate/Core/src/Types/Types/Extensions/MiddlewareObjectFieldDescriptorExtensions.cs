using System.Diagnostics.CodeAnalysis;
using HotChocolate.Resolvers;

namespace HotChocolate.Types;

public static class MiddlewareObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Adds class-based batch middleware to the field. The descriptor must not be <c>null</c>.
    /// </summary>
    /// <remarks>
    /// An <see cref="ObjectFieldDescriptorAttribute"/> can call this method from
    /// its configuration override in attribute-based and source-generated schemas.
    /// </remarks>
    public static IObjectFieldDescriptor UseBatch<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IObjectFieldDescriptor descriptor)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.UseBatch(BatchFieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    /// <summary>
    /// Adds factory-created batch middleware to the field.
    /// The descriptor and factory must not be <c>null</c>.
    /// </summary>
    public static IObjectFieldDescriptor UseBatch<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IObjectFieldDescriptor descriptor,
        Func<IServiceProvider, BatchFieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(factory);

        return descriptor.UseBatch(BatchFieldClassMiddlewareFactory.Create(factory));
    }

    public static IObjectFieldDescriptor Use<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IObjectFieldDescriptor descriptor)
        where TMiddleware : class
    {
        return descriptor.Use(FieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    public static IObjectFieldDescriptor Use<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IObjectFieldDescriptor descriptor,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return descriptor.Use(FieldClassMiddlewareFactory.Create(factory));
    }
}
