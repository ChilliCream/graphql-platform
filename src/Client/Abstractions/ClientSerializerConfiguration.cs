using System.Linq;
using System.Collections.Generic;

namespace StrawberryShake.Configuration
{
    public class ClientSerializerConfiguration
    {
        public Dictionary<string, IValueSerializer> ValueSerializers { get; } =
            Serializers.ValueSerializers.All.ToDictionary(t => t.Name);

        public Dictionary<string, IResultParser> ResultParsers { get; } =
            new Dictionary<string, IResultParser>();

        public IOperationFormatter? OperationFormatter { get; set; }
    }
}
