using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// <code>
/// directive @policy(policies: [[Policy!]!]!) on
///     ENUM
///   | FIELD_DEFINITION
///   | INTERFACE
///   | OBJECT
///   | SCALAR
/// </code>
///
/// Indicates to composition that the target element is restricted based on authorization policies that are evaluated in a Rhai script or coprocessor.
/// Refer to the <see href = "https://www.apollographql.com/docs/router/configuration/authorization#policy"> Apollo Router article</see> for additional details.
/// <example>
/// type Foo @key(fields: "id") {
///   id: ID
///   description: String @policy(policies: [["policy1"]])
/// }
/// </example>
/// </summary>
/// <remarks>
/// Initializes new instance of <see cref="PolicyAttribute"/>
/// </remarks>
/// <param name="policies">
/// Array of required authentication policies.
/// </param>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Struct,
    AllowMultiple = true
)]
public sealed class PolicyAttribute(string[] policies) : DescriptorAttribute
{
    /// <summary>
    /// Retrieves array of required authentication policies.
    /// </summary>
    public string[] Policies { get; } = policies;

    protected internal override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        switch (descriptor)
        {
            case IEnumTypeDescriptor desc:
                desc.Policy(Policies);
                break;

            case IObjectTypeDescriptor desc:
                desc.Policy(Policies);
                break;

            case IObjectFieldDescriptor desc:
                desc.Policy(Policies);
                break;

            case IInterfaceTypeDescriptor desc:
                desc.Policy(Policies);
                break;

            case IInterfaceFieldDescriptor desc:
                desc.Policy(Policies);
                break;
        }
    }
}
