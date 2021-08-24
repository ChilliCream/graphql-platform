using ChilliCream.Testing;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class SchemaHelperTests
    {
        [Fact]
        public void LoadGitHubSchema()
        {
            // arrange
            string schemaSdl = FileResource.Open("GitHub.graphql");
            string extensionsSdl = @"extend schema @key(fields: ""id"")";

            // act
            ISchema schema =
                SchemaHelper.Load(
                    new GraphQLFile[] 
                    {
                        new("GitHub.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                        new("GitHub.extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl))
                    });

            // assert
            ScalarType scalarType = schema.GetType<ScalarType>("X509Certificate");

            Assert.Equal(
                "global::System.String",
                scalarType.ContextData["StrawberryShake.SerializationType"]);

            Assert.Equal(
                "global::System.String",
                scalarType.ContextData["StrawberryShake.RuntimeType"]);
        }

    }
}
