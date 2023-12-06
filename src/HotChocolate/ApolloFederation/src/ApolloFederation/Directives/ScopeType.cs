using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

/// <summary>
/// The <code>Scope</code> scalar representing a JWT scope. Serializes as a string.
/// </summary>
public sealed class ScopeType : ScalarType<Scope, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ScopeType"/>.
    /// </summary>
    public ScopeType() : this(WellKnownTypeNames.Scope)
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

    protected override Scope ParseLiteral(StringValueNode valueSyntax)
        => new Scope(valueSyntax.Value);

    public override IValueNode ParseResult(object? resultValue)
        => ParseValue(resultValue);

    protected override StringValueNode ParseValue(Scope runtimeValue)
        => new StringValueNode(runtimeValue.Value);
}

/// <summary>
/// Scalar <code>Scope</code> representation.
/// </summary>
public sealed class Scope
{
    /// <summary>
    /// Initializes a new instance of <see cref="Scope"/>.
    /// </summary>
    /// <param name="value">
    /// Scope value
    /// </param>
    public Scope(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Retrieve scope value
    /// </summary>
    public string Value { get; }
}
