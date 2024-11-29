namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class Data
{
    public List<Product> Products { get; } =
    [
        new("apollo-federation", "federation", "@apollo/federation", "OSS"),
        new("apollo-studio", "studio", string.Empty, "platform"),
    ];
}
