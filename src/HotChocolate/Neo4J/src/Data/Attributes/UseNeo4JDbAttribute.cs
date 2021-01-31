using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J
{
    public class UseNeo4JDatabaseAttribute
        : ObjectFieldDescriptorAttribute
    {
        private readonly string _databaseName;

        public UseNeo4JDatabaseAttribute(string databaseName) {
            _databaseName = databaseName;
        }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.UseAsyncSessionWithDatabase(_databaseName);
        }
    }
}
