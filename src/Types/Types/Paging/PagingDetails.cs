using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Paging
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
