namespace HotChocolate.Types;

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Name("Query")
            .Field("person")
            .Resolve(new Person());
    }
}
