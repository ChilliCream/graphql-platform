namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;

public class User
{
    public User(string email)
    {
        Email = email;
    }

    public User(string email, int totalProductsCreated)
    {
        Email = email;
        TotalProductsCreated = totalProductsCreated;
    }

    public string Email { get; private set; }

    public int? TotalProductsCreated { get; private set; }
}
