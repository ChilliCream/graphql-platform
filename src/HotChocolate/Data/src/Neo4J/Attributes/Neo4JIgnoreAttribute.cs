using System;

namespace HotChocolate.Data.Neo4J.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Neo4JIgnoreAttribute : Attribute { }
}
