using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.Language;
using static HotChocolate.ApolloFederation.Properties.FederationResources;
using static HotChocolate.ApolloFederation.Constants.WellKnownContextData;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Adds the @key directive which is used to indicate a combination of fields that
    /// can be used to uniquely identify and fetch an object or interface.
    /// <example>
    /// type Product @key(fields: "upc") {
    ///   upc: UPC!
    ///   name: String
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The object type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEntityResolverDescriptor<T> Key<T>(
        this IObjectTypeDescriptor<T> descriptor,
        string fieldSet)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrEmpty(fieldSet))
        {
            throw new ArgumentException(
                FieldDescriptorExtensions_Key_FieldSet_CannotBeNullOrEmpty,
                nameof(fieldSet));
        }

        descriptor.Directive(
            WellKnownTypeNames.Key,
            new ArgumentNode(
                WellKnownArgumentNames.Fields,
                new StringValueNode(fieldSet)));

        return new EntityResolverDescriptor<T>(descriptor);
    }

    /// <summary>
    /// Mark the type as an extension
    /// of a type that is defined by another service when
    /// using apollo federation.
    /// </summary>
    public static IObjectTypeDescriptor<T> ExtendServiceType<T>(
        this IObjectTypeDescriptor<T> descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCreate(d => d.ContextData[ExtendMarker] = true);

        return descriptor;
    }
}
