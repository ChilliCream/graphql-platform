using System.Runtime.CompilerServices;

namespace StrawberryShake.Serialization
{
    public class StringSerializer : ScalarSerializer<string>
    {
        public StringSerializer(string typeName = BuiltInTypeNames.String)
            : base(typeName)
        {
        }
    }
}
