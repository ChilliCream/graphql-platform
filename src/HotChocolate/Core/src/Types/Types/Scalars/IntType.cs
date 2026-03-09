using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// The Int scalar type represents a signed 32‐bit numeric non‐fractional
/// value. Response formats that support a 32‐bit integer or a number type
/// should use that type to represent this scalar.
/// </para>
/// <para>http://facebook.github.io/graphql/June2018/#sec-Int</para>
/// </summary>
public class IntType : IntegerTypeBase<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    public IntType(int min, int max)
        : this(
            ScalarNames.Int,
            TypeResources.IntType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    public IntType(
        string name,
        string? description = null,
        int min = int.MinValue,
        int max = int.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IntType()
        : this(int.MinValue, int.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override int OnCoerceInputLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt32();

    /// <inheritdoc />
    protected override int OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt32();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(int runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(int runtimeValue)
        => new IntValueNode(runtimeValue);
}
