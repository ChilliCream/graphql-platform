using System;
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
        : ScalarType
    {
        public IntType()
            : base("Int")
        {
        }

        public override string Description =>
            TypeResources.IntType_Description();

        public override Type ClrType => typeof(int);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public override IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object Serialize(object value)
        {
            throw new NotImplementedException();
        }

        public override object Deserialize(object value)
        {
            throw new NotImplementedException();
        }

        protected override int OnParseLiteral(IntValueNode node) =>
            int.Parse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);

        protected override IntValueNode OnParseValue(int value) =>
            new IntValueNode(value.ToString("D", CultureInfo.InvariantCulture));
    }
}
