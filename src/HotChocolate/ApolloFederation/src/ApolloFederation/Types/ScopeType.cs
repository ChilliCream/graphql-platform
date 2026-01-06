using System.Text.Json;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The <code>Scope</code> scalar representing a JWT scope. Serializes as a string.
/// </summary>
public sealed class ScopeType : ScalarType<Scope, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ScopeType"/>.
    /// </summary>
    public ScopeType() : this(FederationTypeNames.ScopeType_Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ScopeType"/>.
    /// </summary>
    /// <param name="name">
    /// Scalar name
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar shall bind implicitly to <see cref="Scope"/>.
    /// </param>
    public ScopeType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = FederationResources.ScopeType_Description;
    }

    public override object CoerceInputLiteral(StringValueNode valueLiteral)
        => new Scope(valueLiteral.Value);

    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.String)
        {
            return new Scope(inputValue.GetString()!);
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    public override void CoerceOutputValue(Scope runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.Value);

    public override IValueNode ValueToLiteral(Scope runtimeValue)
        => new StringValueNode(runtimeValue.Value);
}
