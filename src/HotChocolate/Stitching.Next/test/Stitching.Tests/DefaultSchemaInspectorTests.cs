
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding;

public class DefaultSchemaInspectorTests
{
    [Fact]
    public void Query_Operation_Is_Correctly_Discovered()
    {
        // arrange
        var schema = 
            @"schema { query: Query }

            type Query {
                personById(id: ID! @is(a: ""Person.id"")) : Person
            }
            
            type Person {
                id: ID!
                name: String!
            }";

        // act
        var inspector = new DefaultSchemaInspector();
        var schemaInfo = inspector.Inspect(Utf8GraphQLParser.Parse(schema));

        // assert
        Assert.NotNull(schemaInfo.Query);
        Assert.Null(schemaInfo.Mutation);
        Assert.Null(schemaInfo.Subscription);
        Assert.Collection(schemaInfo.Types.Values, t => Assert.Equal("Person", t.Name.Value));
    }
}