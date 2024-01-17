using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @override(from: String!) on FIELD_DEFINITION
/// </code>
///
/// The @override directive is used to indicate that the current subgraph is taking
/// responsibility for resolving the marked field away from the subgraph specified in the from
/// argument. Name of the subgraph to be overridden has to match the name of the subgraph that
/// was used to publish their schema.
///
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID!
///   description: String @override(from: "BarSubgraph")
/// }
/// </example>
/// </summary>
public sealed class OverrideAttribute : ObjectFieldDescriptorAttribute
{

    /// <summary>
    /// Initializes new instance of <see cref="OverrideAttribute"/>
    /// </summary>
    /// <param name="from">
    /// Name of the subgraph to be overridden
    /// </param>
    public OverrideAttribute(string from)
    {
        From = from;
    }

    /// <summary>
    /// Get name of the subgraph to be overridden.
    /// </summary>
    public string From { get; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Override(From);
    }
}
