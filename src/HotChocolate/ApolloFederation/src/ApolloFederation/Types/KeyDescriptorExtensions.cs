using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Descriptors;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public static class KeyDescriptorExtensions
{
    /// <summary>
    /// Applies the @key directive which is used to indicate a combination of fields that can be used to uniquely
    /// identify and fetch an object or interface. The specified field set can represent single field (e.g. "id"),
    /// multiple fields (e.g. "id name") or nested selection sets (e.g. "id user { name }"). Multiple keys can
    /// be specified on a target type.
    ///
    /// Keys can be marked as non-resolvable which indicates to router that given entity should never be
    /// resolved within given subgraph. This allows your subgraph to still reference target entity without
    /// contributing any fields to it.
    /// <example>
    /// type Foo @key(fields: "id") {
    ///   id: ID!
    ///   field: String
    ///   bars: [Bar!]!
    /// }
    ///
    /// type Bar @key(fields: "id", resolvable: false) {
    ///   id: ID!
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
    /// <param name="resolvable">
    /// Boolean flag to indicate whether this entity is resolvable locally.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="fieldSet"/> is <c>null</c> or <see cref="string.Empty"/>.
    /// </exception>
    public static IEntityResolverDescriptor Key(
        this IObjectTypeDescriptor descriptor,
        string fieldSet,
        bool? resolvable = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);

        var arguments = CreateKeyArgumentNodes(fieldSet, resolvable);

        descriptor.Directive(WellKnownTypeNames.Key, arguments);

        return new EntityResolverDescriptor<object>(descriptor);
    }

    /// <inheritdoc cref="Key(IObjectTypeDescriptor)"/>
    public static IInterfaceTypeDescriptor Key(
        this IInterfaceTypeDescriptor descriptor,
        string fieldSet,
        bool? resolvable = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrEmpty(fieldSet);

        var arguments = CreateKeyArgumentNodes(fieldSet, resolvable);

        return descriptor.Directive(WellKnownTypeNames.Key, arguments);
    }

    private static ArgumentNode[] CreateKeyArgumentNodes(string fieldSet, bool? resolvable)
    {
        var notResolvable = resolvable is false;

        var argumentCount = 1;
        if (notResolvable)
        {
            argumentCount++;
        }

        var arguments = new ArgumentNode[argumentCount];
        arguments[0] = new ArgumentNode(
            WellKnownArgumentNames.Fields,
            new StringValueNode(fieldSet));
        
        if (notResolvable)
        {
            arguments[1] =
                new ArgumentNode(
                    WellKnownArgumentNames.Resolvable,
                    new BooleanValueNode(false));
        }

        return arguments;
    }
}