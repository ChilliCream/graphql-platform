namespace HotChocolate.Types;

[InterfaceType]
public class Product
{
    public required string Id { get; set; }
}

[InterfaceType<Product>]
public static partial class ProductType
{
    public static string Kind() => "Product";
}

[ObjectType]
public class Book : Product
{
    public required string Title { get; set; }
}

[QueryType]
public static partial class Query
{
    public static Product GetProduct() => new Book { Id = "1", Title = "GraphQL in Action" };
}
