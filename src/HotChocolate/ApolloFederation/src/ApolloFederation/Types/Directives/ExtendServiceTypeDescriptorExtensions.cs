namespace HotChocolate.ApolloFederation.Types;

public static class ExtendServiceTypeDescriptorExtensions
{
    /// <summary>
    /// Applies @extends directive which is used to represent type extensions in the schema. Federated extended types should have
    /// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
    ///
    /// NOTE: Federation v2 no longer requires `@extends` directive due to the smart entity type merging. All usage of @extends
    /// directive should be removed from your Federation v2 schemas.
    /// <example>
    /// # extended from the Users service
    /// type Foo @extends @key(fields: "id") {
    ///   id: ID
    ///   description: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor<T> ExtendServiceType<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.Extend().Context.GetFederationVersion() == FederationVersion.Federation10)
        {
            descriptor.Directive(ExtendServiceTypeDirective.Default);
        }

        return descriptor;
    }

    /// <summary>
    /// Applies @extends directive which is used to represent type extensions in the schema. Federated extended types should have
    /// corresponding @key directive defined that specifies primary key required to fetch the underlying object.
    ///
    /// NOTE: Federation v2 no longer requires `@extends` directive due to the smart entity type merging. All usage of @extends
    /// directive should be removed from your Federation v2 schemas.
    /// <example>
    /// # extended from the Users service
    /// type Foo @extends @key(fields: "id") {
    ///   id: ID
    ///   description: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <returns>
    /// Returns the object type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectTypeDescriptor ExtendServiceType(
        this IObjectTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.Extend().Context.GetFederationVersion() == FederationVersion.Federation10)
        {
            descriptor.Directive(ExtendServiceTypeDirective.Default);
        }

        return descriptor;
    }
}
