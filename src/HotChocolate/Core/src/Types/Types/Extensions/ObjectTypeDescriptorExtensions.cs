using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions to <see cref="IObjectTypeDescriptor"/>.
/// </summary>
public static class ObjectTypeDescriptorExtensions
{
    public static IObjectTypeDescriptor<T> Ignore<T>(
        this IObjectTypeDescriptor<T> descriptor,
        Expression<Func<T, object>> propertyOrMethod)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (propertyOrMethod is null)
        {
            throw new ArgumentNullException(nameof(propertyOrMethod));
        }

        descriptor.Field(propertyOrMethod).Ignore();
        return descriptor;
    }

    /// <summary>
    /// Specifies an interface that is implemented by this <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor.
    /// </param>
    /// <param name="typeName">
    /// The type name of the interface.
    /// </param>
    public static IObjectTypeDescriptor Implements(
        this IObjectTypeDescriptor descriptor,
        string typeName)
        => descriptor.Implements(new NamedTypeNode(typeName));

    /// <summary>
    /// Specifies an interface that is implemented by this <see cref="ObjectType"/>.
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor.
    /// </param>
    /// <param name="typeName">
    /// The type name of the interface.
    /// </param>
    public static IObjectTypeDescriptor<T> Implements<T>(
        this IObjectTypeDescriptor<T> descriptor,
        string typeName)
        => descriptor.Implements(new NamedTypeNode(typeName));
}
