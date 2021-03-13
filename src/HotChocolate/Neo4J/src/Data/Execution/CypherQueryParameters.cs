using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J
{
    // ReSharper disable once CA1710
    public class CypherQueryParameters : Dictionary<string, object>
    {
        public CypherQueryParameters(object parameters)
        {
            this.WithParams(parameters);
        }

        public CypherQueryParameters()
        {
        }

        public static readonly Func<(string key, object value), object> ValueConvert = o => o.value;
    }
}
