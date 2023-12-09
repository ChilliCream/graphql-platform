using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// <code>
/// # federation v1 definition
/// directive @key(fields: _FieldSet!) repeatable on OBJECT | INTERFACE
///
/// # federation v2 definition
/// directive @key(fields: FieldSet!, resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
/// </code>
///
/// The @key directive is used to indicate a combination of fields that can be used to uniquely
/// identify and fetch an object or interface. The specified field set can represent single field (e.g. "id"),
/// multiple fields (e.g. "id name") or nested selection sets (e.g. "id user { name }"). Multiple keys can
/// be specified on a target type.
///
/// Keys can also be marked as non-resolvable which indicates to router that given entity should never be
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
/// <see cref="NonResolvableKeyAttribute"/>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class KeyAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="KeyAttribute"/>.
    /// </summary>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    public KeyAttribute(string? fieldSet = default)
    {
        FieldSet = fieldSet;
    }

    /// <summary>
    /// Gets the field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </summary>
    public string? FieldSet { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectTypeDescriptor objectTypeDescriptor &&
            element is Type objectType)
        {
            if (string.IsNullOrEmpty(FieldSet))
            {
                throw Key_FieldSet_CannotBeEmpty(objectType);
            }

            objectTypeDescriptor.Key(FieldSet);
        }

        if (descriptor is IObjectFieldDescriptor objectFieldDescriptor &&
            element is MemberInfo)
        {
            objectFieldDescriptor
                .Extend()
                .OnBeforeCreate(d => d.ContextData[Constants.WellKnownContextData.KeyMarker] = true);
        }
    }
}
