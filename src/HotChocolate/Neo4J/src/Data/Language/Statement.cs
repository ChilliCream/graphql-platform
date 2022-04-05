using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public abstract class Statement : Visitable
    {
        public override ClauseKind Kind => ClauseKind.Statement;

        public abstract Dictionary<string, object> GetParameters();

        public abstract List<string> GetParameterNames();

        public static StatementBuilder Builder() => new();
    }
}
