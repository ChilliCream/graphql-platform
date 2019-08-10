using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public class IntTypeBase
        : ScalarType
    {
        private readonly int _min;
        private readonly int _max;

        protected IntTypeBase(NameString name)
            : this(name, int.MinValue, int.MaxValue)
        {
        }

        protected IntTypeBase(NameString name, int min, int max)
            : base(name)
        {
            _min = min;
            _max = max;
        }

        public override Type ClrType => typeof(int);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            return literal is IntValueNode intLiteral
                && int.TryParse(
                    intLiteral.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int i)
                && i >= _min && i <= _max;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is IntValueNode intLiteral
                && int.TryParse(
                    intLiteral.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int i)
                && i >= _min && i <= _max)
            {
                return i;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is int i && i >= _min && i <= _max)
            {
                return true;
            }

            return false;
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is int i && i >= _min && i <= _max)
            {
                return new IntValueNode(i);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int i && i >= _min && i <= _max)
            {
                return i;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is int i && i >= _min && i <= _max)
            {
                value = serialized;
                return true;
            }

            if (TryConvertSerialized(serialized, ScalarValueKind.Integer, out int c)
                && c >= _min && c <= _max)
            {
                value = c;
                return true;
            }

            value = null;
            return false;
        }
    }
}
