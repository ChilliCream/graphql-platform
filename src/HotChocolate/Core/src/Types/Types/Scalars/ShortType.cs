using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

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
            : base(name, min, max, BindingBehavior.Implicit)
        {
        }

        public ShortType(NameString name, string description, short min, short max)
            : base(name, min, max, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override short ParseLiteral(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt16();
        }

        protected override IntValueNode ParseValue(short runtimeValue)
        {
            return new IntValueNode(runtimeValue);
        }
    }
}
