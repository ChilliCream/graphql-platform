using System;

namespace StrawberryShake.Serialization
{
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
}
