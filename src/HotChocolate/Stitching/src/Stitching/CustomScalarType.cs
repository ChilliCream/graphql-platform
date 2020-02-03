using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public sealed class CustomScalarType
        : ScalarType<string, StringValueNode>
    {
        public CustomScalarType(NameString name, string description)
            : base(name, BindingBehavior.Explicit)
        {
            Description = description;
        }

        protected override string ParseLiteral(StringValueNode literal)
        {
            return literal.Value;
        }

        protected override StringValueNode ParseValue(string value)
        {
            return new StringValueNode(value);
        }
    }
}
