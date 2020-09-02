using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class LongType
        : IntegerTypeBase<long>
    {
        public LongType()
            : this(long.MinValue, long.MaxValue)
        {
        }

        public LongType(long min, long max)
            : this(ScalarNames.Long, min, max)
        {
            Description = TypeResources.LongType_Description;
        }

        public LongType(NameString name)
            : this(name, long.MinValue, long.MaxValue)
        {
        }

        public LongType(NameString name, long min, long max)
            : base(name, min, max, BindingBehavior.Implicit)
        {
        }

        public LongType(NameString name, string description, long min, long max)
            : base(name, min, max, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override long ParseLiteral(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt64();
        }

        protected override IntValueNode ParseValue(long runtimeValue)
        {
            return new IntValueNode(runtimeValue);
        }
    }
}
