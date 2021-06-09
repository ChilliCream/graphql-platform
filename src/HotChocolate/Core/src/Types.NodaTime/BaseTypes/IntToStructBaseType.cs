using HotChocolate.Language;
using System.Diagnostics.CodeAnalysis;
using static HotChocolate.Types.NodaTime.Properties.NodaTimeResources;

namespace HotChocolate.Types.NodaTime
{
    public abstract class IntToStructBaseType<TRuntimeType>
        : ScalarType<TRuntimeType, IntValueNode>
        where TRuntimeType : struct
    {
        protected IntToStructBaseType(string name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        protected override TRuntimeType ParseLiteral(IntValueNode literal)
        {
            if (TryDeserialize(literal.ToInt32(), out TRuntimeType? value))
            {
                return value.Value;
            }

            throw new SerializationException(
                string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
                this);
        }

        protected override IntValueNode ParseValue(TRuntimeType value)
        {
            if (TrySerialize(value, out var val))
            {
                return new IntValueNode(val.Value);
            }

            throw new SerializationException(
                string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
                this);
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is int s)
            {
                return new IntValueNode(s);
            }

            if (resultValue is TRuntimeType v)
            {
                return ParseValue(v);
            }

            throw new SerializationException(
                string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
                this);
        }

        public override bool TrySerialize(
            object? runtimeValue,
            out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is TRuntimeType dt && TrySerialize(dt, out var val))
            {
                resultValue = val.Value;
                return true;
            }

            resultValue = null;
            return false;
        }

        protected abstract bool TrySerialize(
            TRuntimeType runtimeValue,
            [NotNullWhen(true)] out int? resultValue);

        public override bool TryDeserialize(
            object? resultValue,
            out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is int i && TryDeserialize(i, out TRuntimeType? val))
            {
                runtimeValue = val;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        protected abstract bool TryDeserialize(
            int resultValue,
            [NotNullWhen(true)] out TRuntimeType? runtimeValue);
    }
}
