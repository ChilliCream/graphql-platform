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

    public override object CoerceInputLiteral(StringValueNode valueLiteral)
        => new Policy(valueLiteral.Value);

    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.String)
        {
            return new Policy(inputValue.GetString()!);
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    public override void CoerceOutputValue(Policy runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.Value);

    public override IValueNode ValueToLiteral(Policy runtimeValue)
        => new StringValueNode(runtimeValue.Value);
}
