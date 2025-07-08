using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities;

public class SchemaHelperTests
{
    [Fact]
    public void LoadGitHubSchema()
    {
        // arrange
        var schemaSdl = FileResource.Open("GitHub.graphql");
        const string extensionsSdl = @"extend schema @key(fields: ""id"")";

        // act
        var schema =
            SchemaHelper.Load(
                [
                    new("GitHub.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                    new("GitHub.extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl))
                ]);

        // assert
        var scalarType = schema.Types.GetType<ScalarType>("X509Certificate");

        Assert.Equal(
            "global::System.String",
            scalarType.GetSerializationType());

        Assert.Equal(
            "global::System.String",
            scalarType.GetRuntimeType());
    }
}
