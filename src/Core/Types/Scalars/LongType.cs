using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class LongType
        : NumberType<long, IntValueNode>
    {
        public LongType()
            : base("Long")
        {
        }

        protected override long OnParseLiteral(IntValueNode node) =>
            long.Parse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        protected override IntValueNode OnParseValue(long value) =>
            new IntValueNode(value.ToString("D", CultureInfo.InvariantCulture));
    }
}
