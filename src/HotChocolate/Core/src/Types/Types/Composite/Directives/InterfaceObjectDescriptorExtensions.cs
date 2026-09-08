namespace HotChocolate.Types.Composite;

/// <summary>
/// Provides an extension method to apply the <see cref="Composite.InterfaceObject"/>
/// directive with the fluent API to object types.
/// </summary>
public static class InterfaceObjectDescriptorExtensions
{
    /// <summary>
    /// <para>
    /// Declares the object type as a stand-in for an interface defined in another source schema
    /// by applying the @interfaceObject directive to it.
    /// </para>
    /// <para>
    /// <see href="https://graphql.github.io/composite-schemas-spec/draft/#sec--interfaceObject"/>
    /// </para>
    /// </summary>
    /// <param name="descriptor">The object type descriptor.</param>
    /// <returns>The object type descriptor with the <see cref="Composite.InterfaceObject"/> directive applied.</returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor InterfaceObject(this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(Composite.InterfaceObject.Instance);
    }
}
