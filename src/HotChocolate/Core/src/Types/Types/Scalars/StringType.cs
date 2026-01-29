using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

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
    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
        => valueLiteral.Value;

    /// <inheritdoc />
    protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => inputValue.GetString()!;

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue);

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(string runtimeValue)
        => new StringValueNode(runtimeValue);
}
