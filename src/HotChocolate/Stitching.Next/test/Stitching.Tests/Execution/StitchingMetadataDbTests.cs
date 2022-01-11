
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Stitching.SchemaBuilding;
using HotChocolate.Types;
using Moq;
using Xunit;

namespace HotChocolate.Stitching.Execution;

public class StitchingMetadataDbTests
{
    [Fact]
    public async Task Metadata_GetSource_UserId_UserName()
    {
        // arrange
        MergedSchema mergedSchema = CreateSchemaInfo();
        
        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocument(mergedSchema.SchemaInfo.ToSchemaDocument())
            .UseField(_ => _)
            .BuildSchemaAsync();

        var user_id = new Mock<ISelection>();
        var user_name = new Mock<ISelection>();

        user_id
            .Setup(t => t.Field)
            .Returns(schema.GetType<ObjectType>("User").Fields["id"]);

        user_name
            .Setup(t => t.Field)
            .Returns(schema.GetType<ObjectType>("User").Fields["name"]);

        // act
        var metadataDb = new StitchingMetadataDb(
            mergedSchema.Sources, 
            schema, 
            mergedSchema.SchemaInfo);

        NameString source = metadataDb.GetSource(new[] { user_id.Object, user_name.Object });

        // assert
        Assert.Equal("Accounts", source.Value);
    }

    [Fact]
    public async Task Metadata_IsPartOfSource()
    {
        // arrange
        MergedSchema mergedSchema = CreateSchemaInfo();
        
        ISchema schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocument(mergedSchema.SchemaInfo.ToSchemaDocument())
            .UseField(_ => _)
            .BuildSchemaAsync();

        var user_id = new Mock<ISelection>();
        var user_username = new Mock<ISelection>();

        user_id
            .Setup(t => t.Field)
            .Returns(schema.GetType<ObjectType>("User").Fields["id"]);

        user_username
            .Setup(t => t.Field)
            .Returns(schema.GetType<ObjectType>("User").Fields["username"]);

        // act
        var metadataDb = new StitchingMetadataDb(
            mergedSchema.Sources, 
            schema, 
            mergedSchema.SchemaInfo);

        bool isIdAccounts = metadataDb.IsPartOfSource("Accounts", user_id.Object);
        bool isIdReviews = metadataDb.IsPartOfSource("Reviews", user_id.Object);
        bool isUsernameAccounts = metadataDb.IsPartOfSource("Accounts", user_username.Object);
        bool isUsernameReviews = metadataDb.IsPartOfSource("Reviews", user_username.Object);

        // assert
        Assert.True(isIdAccounts);
        Assert.True(isIdAccounts);
        Assert.True(isUsernameAccounts);
        Assert.False(isUsernameReviews);
    }

    private MergedSchema CreateSchemaInfo()
    {
        var schemaA =
            @"schema @schema(name: ""Accounts"") {
                query: Query
            }

            type Query {
                users: [User!]!
                userById(id: ID! @is(a: ""User.id"")): User! @internal
            }

            type User {
                id: ID!
                name: String!
                username: String!
                birthdate: DateTime!
            }";

        var schemaB =
            @"schema @schema(name: ""Reviews"") {
                query: Query
            }

            type Query {
                reviews: [Review!]!
                reviewsByAuthor(authorId: ID! @is(a: ""User.id"")): [Review!]! @internal
                reviewsByProduct(upc: ID! @is(a: ""Product.upc"")): [Review!]! @internal
            }

            type Review {
                id: ID!
                user: User!
                product: Product!
                body: String!
            }
            
            type User {
                id: ID!
                name: String!
            }
            
            type Product {
                upc: ID!
            }";

        var inspector = new SchemaInspector();
        var schemaInfoA = inspector.Inspect(Utf8GraphQLParser.Parse(schemaA));
        var schemaInfoB = inspector.Inspect(Utf8GraphQLParser.Parse(schemaB));

        var schemaNameA = schemaInfoA.Name;
        var schemaNameB = schemaInfoB.Name;

        var merger = new SchemaMerger();
        var mergedSchemaInfo = merger.Merge(new[] { schemaInfoA, schemaInfoB });

        return new MergedSchema(
            new List<NameString>
            {
                schemaNameA,
                schemaNameB
            },
            mergedSchemaInfo);
    }

    private class MergedSchema
    {
        public MergedSchema(List<NameString> sources, SchemaInfo schemaInfo)
        {
            Sources = sources;
            SchemaInfo = schemaInfo;
        }

        public List<NameString> Sources { get; }

        public SchemaInfo SchemaInfo { get; }
    }
}
