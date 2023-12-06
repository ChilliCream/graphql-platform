using HotChocolate.ApolloFederation;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @tag directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Applies @tag directive to annotate fields and types with additional metadata information.
    /// Tagging is commonly used for creating variants of the supergraph using contracts.
    /// <example>
    /// type Foo @tag(name: "internal") {
    ///   id: ID!
    ///   name: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="name">
    /// Tag value to be applied on the target
    /// </param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="name"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor ApolloTag(this IEnumTypeDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IEnumValueDescriptor ApolloTag(this IEnumValueDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IInterfaceFieldDescriptor ApolloTag(this IInterfaceFieldDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IInterfaceTypeDescriptor ApolloTag(this IInterfaceTypeDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IInputObjectTypeDescriptor ApolloTag(this IInputObjectTypeDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IInputFieldDescriptor ApolloTag(this IInputFieldDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IObjectFieldDescriptor ApolloTag(this IObjectFieldDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IObjectTypeDescriptor ApolloTag(this IObjectTypeDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    /// <inheritdoc cref="ApolloTag(IEnumTypeDescriptor, string)"/>
    public static IUnionTypeDescriptor ApolloTag(this IUnionTypeDescriptor descriptor, string name)
    {
        ValidateTagExtensionParams(descriptor, name);
        return descriptor.Directive(new TagValue(name));
    }

    private static void ValidateTagExtensionParams(IDescriptor descriptor, string name)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }
    }
}
