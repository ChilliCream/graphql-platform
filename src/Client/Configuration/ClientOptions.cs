using System.Linq;
using System.Collections.Generic;

namespace StrawberryShake.Configuration
{
    public class ClientOptions
    {
        public IDictionary<string, IValueSerializer> ValueSerializers { get; } =
            Serializers.ValueSerializers.All.ToDictionary(t => t.Name);

        public IDictionary<string, IResultParser> ResultParsers { get; } =
            new Dictionary<string, IResultParser>();

        public IOperationFormatter? OperationFormatter { get; set; }
    }
}
