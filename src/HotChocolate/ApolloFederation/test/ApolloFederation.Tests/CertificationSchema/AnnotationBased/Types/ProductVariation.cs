using HotChocolate.Types.Relay;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;

public class ProductVariation
{
    public ProductVariation(string id)
    {
        Id = id;
    }

    [ID]
    public string Id { get; }
}
