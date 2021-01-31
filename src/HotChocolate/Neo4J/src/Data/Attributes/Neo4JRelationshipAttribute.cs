using System.Reflection;
using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JRelationshipAttribute
        : ObjectFieldDescriptorAttribute
    {
        private readonly string _name;
        private readonly RelationshipDirection _direction;

        public Neo4JRelationshipAttribute(
            string name,
            RelationshipDirection direction)
        {
            _name = name;
            _direction = direction;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x =>
                    x.ContextData.Add(nameof(Neo4JRelationshipAttribute), new {_name, _direction}));
        }
    }
}
