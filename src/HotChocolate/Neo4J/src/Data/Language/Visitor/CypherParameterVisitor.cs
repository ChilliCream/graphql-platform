using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CypherParameterVisitor
    {
        class ParameterInformation
        {
            private readonly HashSet<string> _names;
            private readonly Dictionary<string, object> _values;

            private ParameterInformation(HashSet<string> names, Dictionary<string, object> values)
            {
                _names = names;
                _values = values;
            }

        }
    }
}
