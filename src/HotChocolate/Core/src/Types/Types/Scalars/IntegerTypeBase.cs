using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class IntegerTypeBase<TClrType>
        : ScalarType<TClrType, IntValueNode>
        where TClrType : IComparable
    {
        protected IntegerTypeBase(
            NameString name,
            TClrType min,
            TClrType max,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            MinValue = min;
            MaxValue = max;
        }

        public TClrType MinValue { get; }

        public TClrType MaxValue { get; }

        protected override bool IsInstanceOfType(IntValueNode literal)
        {
            return IsInstanceOfType(ParseLiteral(literal));
        }

        protected override bool IsInstanceOfType(TClrType value)
        {
            if (value.CompareTo(MinValue) < 0 || value.CompareTo(MaxValue) > 0)
            {
                return false;
            }

            return true;
        }

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

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is TClrType casted && IsInstanceOfType(casted))
            {
                runtimeValue = resultValue;
                return true;
            }

            if (TryConvertSerialized(resultValue, ValueKind.Integer, out TClrType c)
                && IsInstanceOfType(c))
            {
                runtimeValue = c;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
