#nullable enable

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Serialization.SchemaDebugFormatter;

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
    protected IReadOnlyDictionary<object, EnumValue> ValueLookup => _valueLookup;

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        return Values.TryGetValue(name, out value);
    }

    /// <inheritdoc />
    public bool TryGetRuntimeValue(string name, [NotNullWhen(true)] out object? runtimeValue)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (Values.TryGetValue(name, out var value))
        {
            runtimeValue = value.Value;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <inheritdoc />
    public bool IsInstanceOfType(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax is NullValueNode)
        {
            return true;
        }

        if (valueSyntax is EnumValueNode ev)
        {
            return Values.ContainsName(ev.Value);
        }

        if (valueSyntax is StringValueNode sv)
        {
            return Values.ContainsName(sv.Value);
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsInstanceOfType(object? runtimeValue)
        => runtimeValue is null || RuntimeType.IsInstanceOfType(runtimeValue);

    /// <inheritdoc />
    public object? ParseLiteral(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax is EnumValueNode evn
            && Values.TryGetValue(evn.Value, out var ev))
        {
            return ev.Value;
        }

        if (valueSyntax is StringValueNode svn
            && Values.TryGetValue(svn.Value, out ev))
        {
            return ev.Value;
        }

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    /// <inheritdoc />
    public IValueNode ParseValue(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (_valueLookup.TryGetValue(runtimeValue, out var enumValue))
        {
            return new EnumValueNode(enumValue.Name);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()),
            this);
    }

    /// <inheritdoc />
    public IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s
            && Values.TryGetValue(s, out var enumValue))
        {
            return new EnumValueNode(enumValue.Name);
        }

        if (_valueLookup.TryGetValue(resultValue, out enumValue))
        {
            return new EnumValueNode(enumValue.Name);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }

    /// <inheritdoc />
    public object? Serialize(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return null;
        }

        if (RuntimeType.IsInstanceOfType(runtimeValue)
            && _valueLookup.TryGetValue(runtimeValue, out var enumValue))
        {
            return enumValue.Name;
        }

        // schema first unbound enum type
        if (RuntimeType == typeof(object))
        {
            var name = _naming.GetEnumValueName(runtimeValue);
            if (Values.TryGetValue(name, out enumValue))
            {
                return enumValue.Name;
            }
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_Serialize(Name),
            this);
    }

    /// <inheritdoc />
    public object? Deserialize(object? resultValue)
    {
        if (TryDeserialize(resultValue, out var runtimeValue))
        {
            return runtimeValue;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_Deserialize(Name),
            this);
    }

    /// <inheritdoc />
    public bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string s
            && Values.TryGetValue(s, out var enumValue))
        {
            runtimeValue = enumValue.Value;
            return true;
        }

        if (_valueLookup.TryGetValue(resultValue, out enumValue))
        {
            runtimeValue = enumValue.Value;
            return true;
        }

        runtimeValue = null;
        return false;
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
