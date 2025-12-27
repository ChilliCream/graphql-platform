using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The String scalar type represents textual data, represented as
/// UTF‐8 character sequences. The String type is most often used
/// by GraphQL to represent free‐form human‐readable text.
///
/// http://facebook.github.io/graphql/June2018/#sec-String
/// </summary>
public class StringType : ScalarType<string, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringType"/> class.
    /// </summary>
    public StringType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public StringType()
        : this(
            ScalarNames.String,
            TypeResources.StringType_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    public override object CoerceInputLiteral(StringValueNode valueLiteral)
        => valueLiteral.Value;

    /// <inheritdoc />
    public override object CoerceInputValue(JsonElement inputValue)
    {
        if (inputValue.ValueKind is JsonValueKind.String)
        {
            return inputValue.GetString()!;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    public override void CoerceOutputValue(string runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue);

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(string runtimeValue)
        => new StringValueNode(runtimeValue);
}
