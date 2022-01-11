using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Moq;
using Xunit;
using static HotChocolate.Stitching.Execution.TestHelper;

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
}
