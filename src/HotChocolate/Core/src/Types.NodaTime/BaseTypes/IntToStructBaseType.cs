using HotChocolate.Language;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.NodaTime
{
    public abstract class IntToStructBaseType<TRuntimeType> : ScalarType<TRuntimeType, IntValueNode>
        where TRuntimeType : struct
    {

        public IntToStructBaseType(string name) : base(name, bind: BindingBehavior.Implicit)
        {
        }

        protected abstract bool TrySerialize(TRuntimeType baseValue, [NotNullWhen(true)] out int? output);
        
        protected abstract bool TryDeserialize(int val, [NotNullWhen(true)] out TRuntimeType? output);

        protected override TRuntimeType ParseLiteral(IntValueNode literal)
        {
            if (TryDeserialize(literal.ToInt32(), out TRuntimeType? value))
            {
                return value.Value;
            }

            throw new SerializationException(
                $"Unable to deserialize integer to {this.Name}", 
                this);
        }

        protected override IntValueNode ParseValue(TRuntimeType value)
        {
            if (TrySerialize(value, out int? val))
            {
                return new IntValueNode(val.Value);
            }
        
            throw new SerializationException(
                $"Unable to deserialize integer to {this.Name}", 
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
                $"Unable to deserialize integer to {this.Name}",
                this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is TRuntimeType dt && TrySerialize(dt, out int? val))
            {
                resultValue = val.Value;
                return true;
            }

            resultValue = null;
            return false;
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is int str && TryDeserialize(str, out TRuntimeType? val))
            {
                runtimeValue = val;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
