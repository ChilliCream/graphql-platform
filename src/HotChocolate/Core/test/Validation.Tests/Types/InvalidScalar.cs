using System;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Validation.Types
{
    public class InvalidScalar : ScalarType<string>
    {
        public InvalidScalar()
            : base("Invalid", BindingBehavior.Explicit)
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            return false;
        }

        public override object? ParseLiteral(IValueNode valueSyntax)
        {
            throw new InvalidOperationException();
        }

        public override IValueNode ParseValue(object? value)
        {
            throw new InvalidOperationException();
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            throw new InvalidOperationException();
        }
    }
}
