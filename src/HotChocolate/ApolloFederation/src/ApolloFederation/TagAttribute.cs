using System.Reflection;
using HotChocolate.Types.Descriptors;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The @tag directive is used to applies arbitrary string
/// metadata to a schema location. Custom tooling can use
/// this metadata during any step of the schema delivery flow,
/// including composition, static analysis, and documentation
///
/// <example>
/// # extended from the Users service
/// extend type User @key(fields: "id") {
///   id: ID! @external
///   email: String @tag(name: "public")
///   customerNotes: String @tag(name: "internal")
/// }
/// </example>
/// </summary>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Property |
    AttributeTargets.Method |
    AttributeTargets.Enum,
    AllowMultiple = true)]
public sealed class TagAttribute : DescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="TagAttribute"/>.
    /// </summary>
    /// <param name="name">
    /// The <paramref name="name"/> applies arbitrary string metadata
    /// to a schema location
    /// </param>
    public TagAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name associated with this tag
    /// </summary>
    public string Name { get; }

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        descriptor.Tag(Name);
    }
}
