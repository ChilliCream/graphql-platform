using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class InvalidScalar : ScalarType<string>
{
    public InvalidScalar()
        : base("Invalid", BindingBehavior.Explicit)
    {
    }

    public override bool IsValueCompatible(IValueNode literal)
    {
        return false;
    }

    public override object? CoerceInputLiteral(IValueNode valueSyntax)
    {
        throw new InvalidOperationException();
    }

    public override IValueNode CoerceInputValue(object? value)
    {
        throw new InvalidOperationException();
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        throw new InvalidOperationException();
    }
}
