using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Neo4J;

public class UseNeo4JDatabaseAttribute
    : ObjectFieldDescriptorAttribute
{
    private readonly string _databaseName;

    public UseNeo4JDatabaseAttribute(string databaseName, [CallerLineNumber] int order = 0)
    {
        _databaseName = databaseName;
        Order = order;
    }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.UseAsyncSessionWithDatabase(_databaseName);
    }
}