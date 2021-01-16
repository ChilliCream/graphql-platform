using System;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// If this is used on a field it will be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Neo4JIgnoreAttribute : Attribute { }
}
