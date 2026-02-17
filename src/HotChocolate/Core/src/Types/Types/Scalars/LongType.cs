using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Long</c> scalar type represents a signed 64-bit integer. It is intended for scenarios
/// where values exceed the range of the built-in <c>Int</c> scalar, such as representing large
/// identifiers, timestamps in milliseconds, file sizes in bytes, or any integer values requiring
/// more than 32 bits.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/long.html">Specification</seealso>
public class LongType : IntegerTypeBase<long>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/long.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    public LongType(long min, long max)
        : this(
            ScalarNames.Long,
            TypeResources.LongType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    public LongType(
        string name,
        string? description = null,
        long min = long.MinValue,
        long max = long.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LongType()
        : this(long.MinValue, long.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override long OnCoerceInputLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt64();

    /// <inheritdoc />
    protected override long OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt64();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(long runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(long runtimeValue)
        => new IntValueNode(runtimeValue);
}
