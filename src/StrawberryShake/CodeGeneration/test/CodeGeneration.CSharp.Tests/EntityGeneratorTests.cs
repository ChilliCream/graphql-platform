using ChilliCream.Testing;
using Xunit;
using static StrawberryShake.CodeGeneration.CSharp.GeneratorTestHelper;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityGeneratorTests
    {
        [Fact]
        public void Generate_ChatClient_ConnectionNotAnEntity()
        {
            AssertResult(
                FileResource.Open("ChatPeopleNodes.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }

        [Fact]
        public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly()
        {
            AssertResult(
                FileResource.Open("ChatSendMessage.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }

        [Fact]
        public void Generate_BookClient_DataOnly_UnionDataTypes()
        {
            AssertResult(
                FileResource.Open("BookUnionQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }

        [Fact]
        public void Generate_BookClient_DataOnly_InterfaceDataTypes()
        {
            AssertResult(
                FileResource.Open("BookInterfaceQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }

        [Fact]
        public void Generate_BookClient_DataInEntity_UnionDataTypes()
        {
            AssertResult(
                FileResource.Open("BookUnionQueryWithEntity.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }
    }
}
