using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Types.NodaTime
{
    public abstract class StringToStructBaseType<TRuntimeType> : ScalarType<TRuntimeType, StringValueNode>
        where TRuntimeType : struct
    {
        public StringToStructBaseType(string name) : base(name, bind: BindingBehavior.Implicit)
        {
        }

        protected abstract string Serialize(TRuntimeType baseValue);
        protected abstract bool TryDeserialize(string str, [NotNullWhen(true)] out TRuntimeType? output);

        protected override TRuntimeType ParseLiteral(StringValueNode literal)
        {
            if (TryDeserialize(literal.Value, out TRuntimeType? value))
            {
                return value.Value;
            }

            throw new SerializationException(
                $"Unable to deserialize string to {this.Name}", 
                this);
        }

        protected override StringValueNode ParseValue(TRuntimeType value)
        {
            return new(Serialize(value));
        }

        public override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s)
            {
                return new StringValueNode(s);
            }

            if (resultValue is TRuntimeType v)
            {
                return ParseValue(v);
            }

            throw new SerializationException(
                $"Unable to deserialize string to {this.Name}",
                this);
        }

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is TRuntimeType dt)
            {
                resultValue = Serialize(dt);
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

            if (resultValue is string str && TryDeserialize(str, out TRuntimeType? val))
            {
                runtimeValue = val;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}