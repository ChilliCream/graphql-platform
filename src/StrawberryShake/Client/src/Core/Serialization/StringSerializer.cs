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
}
