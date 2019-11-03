using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class ShortType
        : IntegerTypeBase<short>
    {
        public ShortType()
            : this(short.MinValue, short.MaxValue)
        {
        }

        public ShortType(short min, short max)
            : this(ScalarNames.Short, min, max)
        {
            Description = TypeResources.ShortType_Description;
        }

        public ShortType(NameString name)
            : this(name, short.MinValue, short.MaxValue)
        {
        }

        public ShortType(NameString name, short min, short max)
            : base(name, min, max)
        {
        }

        public ShortType(NameString name, string description, short min, short max)
            : base(name, min, max)
        {
            Description = description;
        }

        protected override short ParseLiteral(IntValueNode literal)
        {
            return literal.ToInt16();
        }

        protected override IntValueNode ParseValue(short value)
        {
            return new IntValueNode(value);
        }
    }
}
