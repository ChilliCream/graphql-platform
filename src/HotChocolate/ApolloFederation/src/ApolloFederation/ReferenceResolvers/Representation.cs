using HotChocolate.Language;

namespace HotChocolate.ApolloFederation;

public class Representation
{
    public NameString TypeName { get; set; }

    public ObjectValueNode Data { get; set; } = default!;
}
