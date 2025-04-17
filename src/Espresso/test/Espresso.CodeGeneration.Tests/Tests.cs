using HotChocolate.Language;

namespace Espresso.CodeGeneration;

public class Tests
{
    [Fact]
    public void Test()
    {
        var schema =
            ModelBuilder.New()
                .AddDocument(
                    """
                    type Query {
                        productById(id: ID!): Product
                    }

                    type Product {
                        id: ID!
                        name: String!
                        price: Float!
                    }
                    """)
                .Build();

        var document =
            Utf8GraphQLParser.Parse(
                """
                query GetProductById($id: ID!) {
                    productById(id: $id) {
                        id
                        name
                        price
                    }
                }
                """);

        var inspector = new OperationInspector(schema);
        var models = inspector.Inspect(document);


        var generator = new CSharpCodeGenerator();
        var snapshot = Snapshot.Create();

        foreach (var (fileName, code) in generator.GenerateCode(models))
        {
            snapshot.Add(code, fileName, "csharp");
        }

        snapshot.MatchMarkdown();
    }
}
