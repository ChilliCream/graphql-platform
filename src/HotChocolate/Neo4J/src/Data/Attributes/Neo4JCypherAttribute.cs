using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class Neo4JCypherAttribute
        : ObjectFieldDescriptorAttribute
    {
        private readonly string _statement;

        public Neo4JCypherAttribute(string statement)
        {
            _statement = statement;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(x =>
                    x.ContextData.Add(nameof(Neo4JCypherAttribute), _statement));
        }
    }
}
