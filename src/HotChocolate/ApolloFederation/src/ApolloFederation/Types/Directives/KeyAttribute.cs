using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.FederationContextData;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

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
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface |
    AttributeTargets.Property,
    AllowMultiple = true)]
public sealed class KeyAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="KeyAttribute"/>.
    /// </summary>
    public KeyAttribute()
    {
        Resolvable = true;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="KeyAttribute"/>.
    /// </summary>
    /// <param name="fieldSet">
    /// The field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </param>
    /// <param name="resolvable">
    /// Indicates whether the key is resolvable.
    /// </param>
    public KeyAttribute(string fieldSet, bool resolvable = true)
    {
        FieldSet = fieldSet;
        Resolvable = resolvable;
    }

    /// <summary>
    /// Gets the field set that describes the key.
    /// Grammatically, a field set is a selection set minus the braces.
    /// </summary>
    public string? FieldSet { get; }

    /// <summary>
    /// Gets a value that indicates whether the key is resolvable.
    /// </summary>
    public bool Resolvable { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (element)
        {
            case Type type:
                ConfigureType(type, descriptor);
                break;

            case PropertyInfo member:
                ConfigureField(member, descriptor);
                break;

            case MethodInfo member:
                ConfigureField(member, descriptor);
                break;
        }
    }

    private void ConfigureType(Type type, IDescriptor descriptor)
    {
        if (string.IsNullOrEmpty(FieldSet))
        {
            throw Key_FieldSet_CannotBeEmpty(type);
        }

        switch (descriptor)
        {
            case IObjectTypeDescriptor typeDesc:
                typeDesc.Key(FieldSet, Resolvable);
                break;

            case IInterfaceTypeDescriptor interfaceDesc:
                interfaceDesc.Key(FieldSet, Resolvable);
                break;
        }
    }

    private void ConfigureField(MemberInfo member, IDescriptor descriptor)
    {
        if (!string.IsNullOrEmpty(FieldSet))
        {
            throw Key_FieldSet_MustBeEmpty(member);
        }

        switch (descriptor)
        {
            case IObjectFieldDescriptor fieldDesc:
                fieldDesc.Extend().Definition.ContextData.TryAdd(KeyMarker, Resolvable);
                break;

            case IInterfaceFieldDescriptor fieldDesc:
                fieldDesc.Extend().Definition.ContextData.TryAdd(KeyMarker, Resolvable);
                break;
        }
    }
}
