using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Serialization.SchemaDebugFormatter;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
/// However, Enum types describe the set of possible values.
/// </para>
/// <para>
/// Enums are not references for a numeric value but are unique values in their own right.
/// They may serialize as a string: the name of the represented value.
/// </para>
/// <para>In this example, an Enum type called Direction is defined:</para>
///
/// <code>
/// enum Direction {
///   NORTH
///   EAST
///   SOUTH
///   WEST
/// }
/// </code>
/// </summary>
public partial class EnumType
    : NamedTypeBase<EnumTypeConfiguration>
    , IEnumTypeDefinition
    , ILeafType
{
    /// <inheritdoc />
    public override TypeKind Kind => TypeKind.Enum;

    /// <summary>
    /// Gets the enum values of this type.
    /// </summary>
    public EnumValueCollection Values => _values;

    IReadOnlyEnumValueCollection IEnumTypeDefinition.Values => Values.AsReadOnlyEnumValueCollection();

    /// <summary>
    /// Gets a dictionary that allows to look up the enum value by its runtime value.
    /// </summary>
    protected internal IReadOnlyDictionary<object, EnumValue> ValueLookup => _valueLookup;

    /// <summary>
    /// Tries to get an enum value by its name.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value to retrieve.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the enum value if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the enum value was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return Values.TryGetValue(name, out value);
    }

    /// <summary>
    /// Tries to get the runtime value of an enum value by its name.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value to retrieve.
    /// </param>
    /// <param name="runtimeValue">
    /// When this method returns, contains the runtime value if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the enum value was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetRuntimeValue(string name, [NotNullWhen(true)] out object? runtimeValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (Values.TryGetValue(name, out var value))
        {
            runtimeValue = value.Value;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <inheritdoc />
    public bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is { Kind: SyntaxKind.EnumValue };

    /// <inheritdoc />
    public bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.String;

    /// <inheritdoc />
    public object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is EnumValueNode enumValueLiteral
            && Values.TryGetValue(enumValueLiteral.Value, out var ev))
        {
            return ev.Value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    public object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.String
            && Values.TryGetValue(inputValue.GetString()!, out var enumValue))
        {
            return enumValue.Value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    public void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
    {
        if (RuntimeType.IsInstanceOfType(runtimeValue)
            && _valueLookup.TryGetValue(runtimeValue, out var enumValue))
        {
            resultValue.SetStringValue(enumValue.Name);
            return;
        }

        // schema first unbound enum type
        else if (RuntimeType == typeof(object))
        {
            var name = _naming.GetEnumValueName(runtimeValue);
            if (Values.TryGetValue(name, out enumValue))
            {
                resultValue.SetStringValue(enumValue.Name);
                return;
            }
        }

        throw Scalar_Cannot_CoerceOutputValue(this, runtimeValue);
    }

    /// <inheritdoc />
    public IValueNode ValueToLiteral(object runtimeValue)
    {
        if (RuntimeType.IsInstanceOfType(runtimeValue)
            && _valueLookup.TryGetValue(runtimeValue, out var enumValue))
        {
            return new EnumValueNode(enumValue.Name);
        }

        // schema first unbound enum type
        else if (RuntimeType == typeof(object))
        {
            var name = _naming.GetEnumValueName(runtimeValue);
            if (Values.TryGetValue(name, out enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }
        }

        throw Scalar_Cannot_CoerceOutputValue(this, runtimeValue);
    }

    /// <summary>
    /// Creates a <see cref="EnumTypeDefinitionNode"/> that represents the enum type.
    /// </summary>
    /// <returns>
    /// The GraphQL syntax node that represents the enum type.
    /// </returns>
    public new EnumTypeDefinitionNode ToSyntaxNode()
        => Format(this);

    /// <inheritdoc />
    protected override ITypeDefinitionNode FormatType()
        => Format(this);
}
