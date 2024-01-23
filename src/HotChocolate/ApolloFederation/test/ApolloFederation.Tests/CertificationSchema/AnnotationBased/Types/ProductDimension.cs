namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;

public class ProductDimension(string size, double weight)
{
    public string? Size { get; } = size;

    public double? Weight { get; } = weight;
}
