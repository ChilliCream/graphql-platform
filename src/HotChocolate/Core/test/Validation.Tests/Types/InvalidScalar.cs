using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class InvalidScalar
        : ScalarType<string>
    {
        public InvalidScalar() 
            : base("Invalid", BindingBehavior.Explicit)
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new InvalidOperationException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new InvalidOperationException();
        }
    }
}
