using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Statement
    {
        public static StatementBuilder Builder() => new();

        public abstract Dictionary<string, object> GetParameters();

        public abstract List<string> GetParameterNames();

        public abstract string GetCypher();
    }
}
