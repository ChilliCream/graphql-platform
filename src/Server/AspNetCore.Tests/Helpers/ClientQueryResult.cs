using System.Collections.Generic;

namespace HotChocolate.AspNetCore
{
    public class ClientQueryResult
    {
        public Dictionary<string, object> Data { get; set; }
        public List<Dictionary<string, object>> Errors { get; set; }
    }
}
