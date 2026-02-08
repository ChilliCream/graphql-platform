using System.Text.Json;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

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

    protected override Policy OnCoerceInputLiteral(StringValueNode valueLiteral)
        => new(valueLiteral.Value);

    protected override Policy OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => new(inputValue.GetString()!);

    protected override void OnCoerceOutputValue(Policy runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.Value);

    protected override StringValueNode OnValueToLiteral(Policy runtimeValue)
        => new(runtimeValue.Value);
}
