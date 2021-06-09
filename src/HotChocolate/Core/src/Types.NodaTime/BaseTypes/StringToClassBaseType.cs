using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using static HotChocolate.Types.NodaTime.Properties.NodaTimeResources;

namespace HotChocolate.Types.NodaTime
{
    public abstract class StringToClassBaseType<TRuntimeType>
        : ScalarType<TRuntimeType, StringValueNode>
        where TRuntimeType : class
    {
        public StringToClassBaseType(string name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        /// <inheritdoc />
        protected override TRuntimeType ParseLiteral(StringValueNode literal)
        {
            if (TryDeserialize(literal.Value, out TRuntimeType? value))
            {
                return value;
            }

            throw new SerializationException(
                string.Format(StringToClassBaseType_ParseLiteral_UnableToDeserializeString, Name),
                this);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(TRuntimeType value) =>
            new(Serialize(value));

        /// <inheritdoc />
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
                string.Format(StringToClassBaseType_ParseLiteral_UnableToDeserializeString, Name),
                this);
        }

        /// <inheritdoc />
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

        protected abstract string Serialize(TRuntimeType runtimeValue);

        /// <inheritdoc />
        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s && TryDeserialize(s, out TRuntimeType? val))
            {
                runtimeValue = val;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        protected abstract bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out TRuntimeType? runtimeValue);
    }
}
