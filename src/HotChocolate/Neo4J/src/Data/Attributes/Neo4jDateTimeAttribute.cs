using System;

namespace HotChocolate.Data.Neo4J
{
    /// <summary>
    /// If this is used on a <see cref="System.DateTime"/> property - it will be serialized as a Neo4j Date object rather than a string
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Neo4JDateTimeAttribute : Attribute { }
}
