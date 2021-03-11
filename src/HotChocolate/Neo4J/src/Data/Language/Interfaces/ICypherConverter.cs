using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public interface ICypherConverter
    {
        public string ToQuery(object value);

        public object ToValue(object neoValue, Type target);
    }
}
