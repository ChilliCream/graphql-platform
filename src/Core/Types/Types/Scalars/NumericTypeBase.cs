using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public abstract class NumericTypeBase<T>
        : ScalarType
    {
        protected NumericTypeBase(NameString name)
            : base(name)
        {
        }

        public override Type ClrType => typeof(T);

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

            if (literal is IntValueNode intLiteral
                && TryParseValue(intLiteral.Value, out _))
            {
                return true;
            }

            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is IntValueNode intLiteral
                && TryParseValue(intLiteral.Value, out T value))
            {
                return value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is T v)
            {
                return new IntValueNode(SerializeValue(v));
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

            if (value is T v)
            {
                return v;
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

            if (serialized is T v)
            {
                value = v;
                return true;
            }

            if (TryConvertSerialized(serialized, ScalarValueKind.Integer, out T c))
            {
                value = c;
                return true;
            }

            value = null;
            return false;
        }

        protected abstract bool TryParseValue(string s, out T value);

        protected abstract string SerializeValue(T value);
    }
}
