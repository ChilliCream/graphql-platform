using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities;

public class SchemaHelperTests
{
    [Fact]
    public void LoadGitHubSchema()
    {
        // arrange
        var schemaSdl = FileResource.Open("GitHub.graphql");
        var extensionsSdl = @"extend schema @key(fields: ""id"")";

        // act
        var schema =
            SchemaHelper.Load(
                new GraphQLFile[]
                {
                    new("GitHub.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                    new("GitHub.extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl)),
                });

        // assert
        var scalarType = schema.GetType<ScalarType>("X509Certificate");

        Assert.Equal(
            "global::System.String",
            scalarType.ContextData["StrawberryShake.SerializationType"]);

        Assert.Equal(
            "global::System.String",
            scalarType.ContextData["StrawberryShake.RuntimeType"]);
    }
}
