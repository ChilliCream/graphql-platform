using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class InvalidScalar : ScalarType<string>
{
    public InvalidScalar()
        : base("Invalid", BindingBehavior.Explicit)
    {
    }

    public override ScalarSerializationType SerializationType => ScalarSerializationType.Undefined;

    public override object CoerceInputLiteral(IValueNode valueLiteral)
        => throw new InvalidOperationException();

    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => throw new InvalidOperationException();

    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
        => throw new InvalidOperationException();

    protected override IValueNode OnValueToLiteral(string runtimeValue)
        => throw new InvalidOperationException();
}
