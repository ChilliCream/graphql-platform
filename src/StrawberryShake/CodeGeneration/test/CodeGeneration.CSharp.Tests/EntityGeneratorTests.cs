using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorTests
    {
        [Fact]
        public void Generate_ChatClient_ConnectionNotAnEntity()
        {
            AssertResult(
                "ChatPeopleNodes.graphql",
                "Schema.extensions.graphql",
                "ChatSchema.graphql");
        }

        [Fact]
        public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly()
        {
            AssertResult(
                "ChatSendMessage.graphql",
                "Schema.extensions.graphql",
                "ChatSchema.graphql");
        }

        [Fact]
        public void Generate_BookClient_DataOnly_UnionDataTypes()
        {
            AssertResult(
                "BookUnionQuery.graphql",
                "Schema.extensions.graphql",
                "BookSchema.graphql");
        }

        [Fact]
        public void Generate_BookClient_DataOnly_InterfaceDataTypes()
        {
            AssertResult(
                "BookInterfaceQuery.graphql",
                "Schema.extensions.graphql",
                "BookSchema.graphql");
        }

        [Fact]
        public void Generate_BookClient_DataInEntity_UnionDataTypes()
        {
            AssertResult(
                "BookUnionQueryWithEntity.graphql",
                "Schema.extensions.graphql",
                "BookSchema.graphql");
        }
    }
}
