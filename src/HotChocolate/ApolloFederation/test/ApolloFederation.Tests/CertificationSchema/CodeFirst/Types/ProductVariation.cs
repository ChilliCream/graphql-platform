using HotChocolate.Types.Relay;

namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class ProductVariation
{
    public ProductVariation(string id)
    {
        Id = id;
    }

    [ID]
    public string Id { get; }
}
