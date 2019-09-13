using System.Collections.Generic;

namespace HotChocolate.AspNetCore.Tests.Utilities
{
    public class ClientQueryResult
    {
        public Dictionary<string, object> Data { get; set; }
        public List<Dictionary<string, object>> Errors { get; set; }
        public Dictionary<string, object> Extensions { get; set; }
    }
}
