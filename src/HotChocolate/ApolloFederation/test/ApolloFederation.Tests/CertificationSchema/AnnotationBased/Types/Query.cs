using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;

[ExtendServiceType]
public class Query
{
    public Product? GetProduct([ID] string id, Data repository)
        => repository.Products.FirstOrDefault(t => t.Id.Equals(id));
}
