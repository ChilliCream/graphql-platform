using System;

namespace HotChocolate.Data.Neo4J
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NodeIdAttribute : Attribute { }
}
