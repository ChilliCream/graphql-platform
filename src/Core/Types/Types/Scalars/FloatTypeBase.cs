using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public abstract class FloatTypeBase<TClrType>
       : ScalarType<TClrType>
       where TClrType : IComparable
    {
        protected FloatTypeBase(NameString name, TClrType min, TClrType max)
           : base(name)
        {
            MinValue = min;
            MaxValue = max;
        }

        public TClrType MinValue { get; }

        public TClrType MaxValue { get; }


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

            if (literal is FloatValueNode floatLiteral && IsInstanceOfType(floatLiteral))
            {
                return true;
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float

            if (literal is IntValueNode intLiteral && IsInstanceOfType(intLiteral))
            {
                return true;
            }

            return false;
        }

        protected virtual bool IsInstanceOfType(IFloatValueLiteral literal)
        {
            return IsInstanceOfType(ParseLiteral(literal));
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

            if (literal is FloatValueNode floatLiteral)
            {
                return ParseLiteral(floatLiteral);
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float

            if (literal is IntValueNode intLiteral)
            {
                return ParseLiteral(intLiteral);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected abstract TClrType ParseLiteral(IFloatValueLiteral literal);

        protected virtual bool IsInstanceOfType(TClrType value)
        {
            if (value.CompareTo(MinValue) == -1 || value.CompareTo(MaxValue) == 1)
            {
                return false;
            }

            return true;
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is TClrType casted && IsInstanceOfType(casted))
            {
                return ParseValue(casted);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        protected abstract FloatValueNode ParseValue(TClrType value);

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is TClrType casted && IsInstanceOfType(casted))
            {
                serialized = value;
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is TClrType casted && IsInstanceOfType(casted))
            {
                value = serialized;
                return true;
            }

            if ((TryConvertSerialized(serialized, ValueKind.Float, out TClrType c)
                || TryConvertSerialized(serialized, ValueKind.Integer, out c))
                && IsInstanceOfType(c))
            {
                value = c;
                return true;
            }

            value = null;
            return false;
        }
    }
}
