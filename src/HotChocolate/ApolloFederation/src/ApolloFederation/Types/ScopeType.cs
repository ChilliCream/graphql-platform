using System.Text.Json;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

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

    protected override Scope OnCoerceInputLiteral(StringValueNode valueLiteral)
        => new(valueLiteral.Value);

    protected override Scope OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => new Scope(inputValue.GetString()!);

    protected override void OnCoerceOutputValue(Scope runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.Value);

    protected override StringValueNode OnValueToLiteral(Scope runtimeValue)
        => new(runtimeValue.Value);
}
