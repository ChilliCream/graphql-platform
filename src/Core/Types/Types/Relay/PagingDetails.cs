using System.Collections.Generic;

namespace HotChocolate.Types.Relay
{
    public class PagingDetails
    {
        public IDictionary<string, object> Properties { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        public int? First { get; set; }
        public int? Last { get; set; }
    }
}
