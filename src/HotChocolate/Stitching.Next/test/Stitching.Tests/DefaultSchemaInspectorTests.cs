
using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding;

public class DefaultSchemaInspectorTests
{
    [Fact]
    public void Single_Fetchers_Are_Correctly_Discoverd()
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

        Assert.Collection(
            schemaInfo.Types.Values,
            t =>
            {
                Assert.Equal("Person", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f => 
                    {
                        Assert.Equal("id", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Person.id", f.Arguments.Single().Binding.ToString());
                    });
            });
    }

    [Fact]
    public void Multiple_Fetchers_Are_Correctly_Discoverd()
    {
        // arrange
        var schema =
            @"schema { query: Query }

            type Query {
                personById(id: ID! @is(a: ""Person.id"")) : Person
                personByAccount(accountId: ID! @is(a: ""Account.id"")) : [Person!]
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

        Assert.Collection(
            schemaInfo.Types.Values,
            t =>
            {
                Assert.Equal("Person", t.Name.Value);
                Assert.Collection(
                    Assert.IsType<ObjectTypeInfo>(t).Fetchers,
                    f => 
                    {
                        Assert.Equal("id", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Person.id", f.Arguments.Single().Binding.ToString());
                    },
                    f => 
                    {
                        Assert.Equal("accountId", f.Arguments.Single().Name);
                        Assert.Equal("ID!", f.Arguments.Single().Type.ToString());
                        Assert.Equal("Account.id", f.Arguments.Single().Binding.ToString());
                    });
            });
    }
}