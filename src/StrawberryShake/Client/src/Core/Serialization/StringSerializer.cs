using System;
using System.Runtime.CompilerServices;

namespace StrawberryShake.Serialization
{
    public class StringSerializer : ScalarSerializer<string, string>
    {
        public StringSerializer(string typeName = BuiltInTypeNames.String)
            : base(typeName)
        {
        }

        public override string Parse(string serializedValue) => serializedValue;

        protected override string Format(string runtimeValue) => runtimeValue;
    }

    public abstract class ScalarSerializer<TSerialized, TRuntime>
        : ILeafValueParser<TSerialized, TRuntime>
        , IInputValueFormatter
    {
        protected ScalarSerializer(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        public string TypeName { get; }

        public abstract TRuntime Parse(TSerialized serializedValue);

        public object? Format(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (runtimeValue is TRuntime r)
            {
                return Format(r);
            }

            throw ThrowHelper.InputFormatter_InvalidType(typeof(TRuntime).FullName, TypeName);
        }

        protected abstract TSerialized Format(TRuntime runtimeValue);
    }

    public class BooleanStringSerializer : ScalarSerializer<bool, bool>
    {
        public BooleanStringSerializer(string typeName) : base(typeName)
        {
        }

        public override bool Parse(bool serializedValue) => serializedValue;

        protected override bool Format(bool runtimeValue) => runtimeValue;
    }

}
