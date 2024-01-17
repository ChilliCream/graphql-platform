using HotChocolate.ApolloFederation;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Provides extensions for applying @policy directive on type system descriptors.
/// </summary>
public static partial class ApolloFederationDescriptorExtensions
{
    /// <summary>
    /// Indicates to composition that the target element is restricted based on authorization policies
    /// that are evaluated in a Rhai script or coprocessor.
    /// <example>
    /// type Foo {
    ///   description: String @policy(policies: [["policy1Or", "policy2"], ["andPolicy3"]])
    /// }
    /// </example>
    /// </summary>
    /// <param name="descriptor">
    /// The type descriptor on which this directive shall be annotated.
    /// </param>
    /// <param name="policies">The policy collection</param>
    /// <returns>
    /// Returns the type descriptor.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IEnumTypeDescriptor Policy(
        this IEnumTypeDescriptor descriptor,
        string[][] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(
            WellKnownTypeNames.PolicyDirective,
            ParsePoliciesArgument(policies));
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, string[][])"/>
    public static IInterfaceFieldDescriptor Policy(
        this IInterfaceFieldDescriptor descriptor,
        string[][] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(
            WellKnownTypeNames.PolicyDirective,
            ParsePoliciesArgument(policies));
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, string[][])"/>
    public static IInterfaceTypeDescriptor Policy(
        this IInterfaceTypeDescriptor descriptor,
        string[][] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(
            WellKnownTypeNames.PolicyDirective,
            ParsePoliciesArgument(policies));
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, string[][])"/>
    public static IObjectFieldDescriptor Policy(
        this IObjectFieldDescriptor descriptor,
        string[][] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(
            WellKnownTypeNames.PolicyDirective,
            ParsePoliciesArgument(policies));
    }

    /// <inheritdoc cref="Policy(IEnumTypeDescriptor, string[][])"/>
    public static IObjectTypeDescriptor Policy(
        this IObjectTypeDescriptor descriptor,
        string[][] policies)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Directive(
            WellKnownTypeNames.PolicyDirective,
            ParsePoliciesArgument(policies));
    }

    private static ArgumentNode ParsePoliciesArgument(string[][] policies)
    {
        var list = PolicyParsingHelper.ParseValue(policies);
        var result = new ArgumentNode(WellKnownArgumentNames.Policies, list);
        return result;
    }
}
