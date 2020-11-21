using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class Parameters
    {
        public Dictionary<string, string> Collection { get; set; } =
            new Dictionary<string, string>();

        public Parameters()
        {
        }

        public void AddParameter(string name, string value)
        {
            Collection.Add(name, value);
        }

    }
}
