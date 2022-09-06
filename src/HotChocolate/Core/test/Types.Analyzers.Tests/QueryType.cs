using System.Threading.Tasks;

namespace HotChocolate.Types;

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Name("Query");

        descriptor
            .Field("person")
            .Type("Entity")
            .Resolve(new Person());

        descriptor
            .Field("enum")
            .Type("CustomEnum")
            .Resolve(_ => new ValueTask<object?>());
    }
}
