using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class IntType
        : NumberType<int, IntValueNode>
    {
        public IntType()
            : base("Int")
        {
        }

        protected override int OnParseLiteral(IntValueNode node) =>
            int.Parse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        protected override IntValueNode OnParseValue(int value) =>
            new IntValueNode(value.ToString("D", CultureInfo.InvariantCulture));
    }
}
