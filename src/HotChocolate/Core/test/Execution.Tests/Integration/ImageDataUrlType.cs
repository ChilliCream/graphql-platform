using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Execution.Integration;

public class ImageDataUrlType : ScalarType<string, StringValueNode>
{
    public ImageDataUrlType()
        : base("ImageDataUrl")
    {
    }

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        return Validate(valueLiteral.Value);
    }

    protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        return Validate(inputValue.GetString()!);
    }

    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
    {
        resultValue.SetStringValue(Validate(runtimeValue));
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
    {
        return new StringValueNode(Validate(runtimeValue));
    }

    private string Validate(string runtimeValue)
    {
        if (!runtimeValue.StartsWith("data:image/", StringComparison.Ordinal))
        {
            throw new LeafCoercionException("The value is not a valid image data URL.", this);
        }

        return runtimeValue;
    }
}
