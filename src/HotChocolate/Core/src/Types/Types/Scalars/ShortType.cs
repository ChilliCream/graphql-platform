using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Short</c> scalar type represents a signed 16-bit integer. It is intended for scenarios
/// where values are constrained to the range -32,768 to 32,767, providing a more compact
/// representation than the built-in <c>Int</c> scalar for smaller integer values.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/short.html">Specification</seealso>
public class ShortType : IntegerTypeBase<short>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/short.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    public ShortType(short min, short max)
        : this(
            ScalarNames.Short,
            TypeResources.ShortType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    public ShortType(
        string name,
        string? description = null,
        short min = short.MinValue,
        short max = short.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ShortType() : this(short.MinValue, short.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override short OnCoerceInputLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt16();

    /// <inheritdoc />
    protected override short OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt16();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(short runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(short runtimeValue)
        => new IntValueNode(runtimeValue);
}
