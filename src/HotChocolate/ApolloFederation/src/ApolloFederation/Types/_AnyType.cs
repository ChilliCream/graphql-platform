using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// The _Any scalar is used to pass representations of entities
/// from external services into the root _entities field for execution.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class _AnyType : ScalarType<Representation, ObjectValueNode>
{
    public const string TypeNameField = "__typename";

    /// <summary>
    /// Initializes a new instance of <see cref="_AnyType"/>.
    /// </summary>
    public _AnyType()
        : this(AnyType_Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="_AnyType"/>.
    /// </summary>
    /// <param name="name">
    /// The name the scalar shall have.
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar shall bind implicitly to <see cref="SelectionSetNode"/>.
    /// </param>
    public _AnyType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = FederationResources.Any_Description;
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(ObjectValueNode valueSyntax)
    {
        return valueSyntax.Fields.Any(field => field.Name.Value == TypeNameField);
    }

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is Representation representation)
        {
            return ParseValue(representation);
        }

        throw Scalar_CannotParseValue(this, resultValue.GetType());
    }

    /// <inheritdoc />
    protected override Representation ParseLiteral(ObjectValueNode valueSyntax)
    {
        if (valueSyntax.Fields.FirstOrDefault(
            field => field.Name.Value.EqualsOrdinal("__typename")) is { Value: StringValueNode s, })
        {
            return new Representation(s.Value, valueSyntax);
        }

        throw Any_InvalidFormat(this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is Representation)
        {
            resultValue = ParseValue(runtimeValue);
            return true;
        }

        resultValue = null;
        return false;
    }

    /// <inheritdoc />
    protected override ObjectValueNode ParseValue(Representation runtimeValue)
    {
        return new ObjectValueNode(runtimeValue.Data.Fields);
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is ObjectValueNode ovn)
        {
            var typeField = ovn.Fields.SingleOrDefault(
                field => field.Name.Value.EqualsOrdinal(TypeNameField));

            if (typeField?.Value is StringValueNode svn)
            {
                runtimeValue = new Representation(svn.Value, ovn);
                return true;
            }

            runtimeValue = null;
            return false;
        }

        runtimeValue = null;
        return false;
    }
}
