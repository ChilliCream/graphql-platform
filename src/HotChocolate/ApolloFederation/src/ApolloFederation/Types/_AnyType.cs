using System.Text.Json;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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

    /// <summary>
    /// Parses a GraphQL object literal into a <see cref="Representation"/>.
    /// The object must contain a <c>__typename</c> field with a string value.
    /// </summary>
    /// <param name="valueLiteral">The GraphQL object value node to parse.</param>
    /// <returns>A <see cref="Representation"/> containing the typename and object data.</returns>
    /// <exception cref="GraphQLException">Thrown when the object is missing the <c>__typename</c> field.</exception>
    protected override Representation OnCoerceInputLiteral(ObjectValueNode valueLiteral)
    {
        if (valueLiteral.Fields.FirstOrDefault(
            field => field.Name.Value.EqualsOrdinal(TypeNameField)) is { Value: StringValueNode s })
        {
            return new Representation(s.Value, valueLiteral);
        }

        throw Any_InvalidFormat(this);
    }

    /// <summary>
    /// Parses a JSON object into a <see cref="Representation"/>.
    /// The object must contain a <c>__typename</c> field with a string value.
    /// </summary>
    /// <param name="inputValue">The JSON element to parse.</param>
    /// <param name="context">The feature provider context.</param>
    /// <returns>A <see cref="Representation"/> containing the typename and object data.</returns>
    /// <exception cref="GraphQLException">
    /// Thrown when the input is not an object or is missing the <c>__typename</c> field.
    /// </exception>
    protected override Representation OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.Object
            && inputValue.TryGetProperty(TypeNameField, out var typeNameElement)
            && typeNameElement.ValueKind is JsonValueKind.String)
        {
            var typeName = typeNameElement.GetString()!;
            var parser = new JsonValueParser();
            var objectNode = (ObjectValueNode)parser.Parse(inputValue);
            return new Representation(typeName, objectNode);
        }

        throw Any_InvalidFormat(this);
    }

    /// <summary>
    /// This operation is not supported. The _Any scalar is an input-only type used for
    /// receiving entity representations from federated services.
    /// </summary>
    /// <param name="runtimeValue">The runtime value (not used).</param>
    /// <param name="resultValue">The result element (not used).</param>
    /// <exception cref="NotSupportedException">Always thrown as output coercion is not supported.</exception>
    protected override void OnCoerceOutputValue(Representation runtimeValue, ResultElement resultValue)
        => throw new NotSupportedException(
            "The _Any scalar is an input-only type and does not support output coercion.");

    /// <summary>
    /// Converts a <see cref="Representation"/> back to a GraphQL object value node.
    /// </summary>
    /// <param name="runtimeValue">The representation to convert.</param>
    /// <returns>An <see cref="ObjectValueNode"/> containing the representation data.</returns>
    protected override ObjectValueNode OnValueToLiteral(Representation runtimeValue)
        => new(runtimeValue.Data.Fields);
}
