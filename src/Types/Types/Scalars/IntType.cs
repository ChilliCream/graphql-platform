using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional
    /// value. Response formats that support a 32‐bit integer or a number type
    /// should use that type to represent this scalar.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Int
    /// </summary>
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
