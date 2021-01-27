using System;
using HotChocolate.Data.Neo4J.Language;

namespace HotChocolate.Data.Neo4J
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Neo4JRelationshipAttribute : Attribute
    {
        public string Name { get; set; }
        public RelationshipDirection Direction { get; set; } = RelationshipDirection.Incoming;

        public Neo4JRelationshipAttribute(string name, RelationshipDirection direction)
        {
            Name = name;
            Direction = direction;
        }
    }
}
