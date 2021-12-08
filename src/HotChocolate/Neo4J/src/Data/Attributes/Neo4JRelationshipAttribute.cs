using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JRelationshipAttribute
        : ObjectFieldDescriptorAttribute
    {
        public Neo4JRelationshipAttribute(string name)
        {
            Name = name;
        }

        public Neo4JRelationshipAttribute(
            string name,
            RelationshipDirection direction)
        {
            Name = name;
            Direction = direction;
        }

        public string Name { get; }

        public RelationshipDirection Direction { get; } = RelationshipDirection.Outgoing;

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x => x.ContextData.Add(nameof(Neo4JRelationshipAttribute), this));
        }
    }
}
