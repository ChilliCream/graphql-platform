using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional
    /// value. Response formats that support a 32‐bit integer or a number type
    /// should use that type to represent this scalar.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Int
    /// </summary>
    [SpecScalar]
    public sealed class IntType
        : IntegerTypeBase<int>
    {
        public IntType()
            : this(int.MinValue, int.MaxValue)
        {
        }

        public IntType(int min, int max)
            : this(ScalarNames.Int, min, max)
        {
            Description = TypeResources.IntType_Description;
        }

        public IntType(NameString name)
            : this(name, int.MinValue, int.MaxValue)
        {
        }

        public IntType(NameString name, int min, int max)
            : base(name, min, max)
        {
        }

        public IntType(NameString name, string description, int min, int max)
            : base(name, min, max)
        {
            Description = description;
        }

        protected override int ParseLiteral(IntValueNode literal)
        {
            return literal.ToInt32();
        }

        protected override IntValueNode ParseValue(int value)
        {
            return new IntValueNode(value);
        }
    }
}
