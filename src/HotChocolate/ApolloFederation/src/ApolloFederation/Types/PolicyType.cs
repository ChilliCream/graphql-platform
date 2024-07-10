using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The <code>Policy</code> scalar representing an authorization policy. Serializes as a string.
/// </summary>
public sealed class PolicyType : ScalarType<Policy, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="PolicyType"/>.
    /// </summary>
    public PolicyType() : this(FederationTypeNames.PolicyType_Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PolicyType"/>.
    /// </summary>
    /// <param name="name">
    /// Scalar name
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar shall bind implicitly to <see cref="Policy"/>.
    /// </param>
    public PolicyType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = FederationResources.PolicyType_Description;
    }

    protected override Policy ParseLiteral(StringValueNode valueSyntax)
        => new(valueSyntax.Value);

    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);

    protected override StringValueNode ParseValue(Policy runtimeValue)
        => new(runtimeValue.Value);
}
