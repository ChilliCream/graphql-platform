namespace HotChocolate.Fusion.Types;

public class CompositeInterfaceTypeTests : FusionTestBase
{
    [Fact]
    public void Simple_Interface()
    {
        var schema = CreateCompositeSchema(
            """
            interface Node
                @fusion__type(schema: ACCOUNTS) {
                id: ID!
                    @fusion__field(schema: ACCOUNTS)
            }

            type Product implements Node
                @fusion__type(schema: ACCOUNTS) {
                id: ID!
                    @fusion__field(schema: ACCOUNTS)
                name: String
                    @fusion__field(schema: ACCOUNTS)
            }

            type Query
                @fusion__type(schema: ACCOUNTS) {
                node(id: ID!): Node
            }
            """);

        var node = schema.GetType<CompositeInterfaceType>("Node");
        Assert.Equal("Node", node.Name);
        Assert.Equal("ACCOUNTS", Assert.Single(node.Sources).SchemaName);
        Assert.Equal("id", Assert.Single(node.Fields).Name);

        var product = schema.GetType<CompositeObjectType>("Product");
        Assert.Equal("Product", product.Name);
        Assert.Equal("ACCOUNTS", Assert.Single(node.Sources).SchemaName);
        Assert.Equal("node", Assert.Single(node.Implements).Name);
    }

    [Fact]
    public void Interface_With_Lookup()
    {

    }

}
