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
#if !NETCOREAPP3_1
        [Fact]
        public void Generate_ChatClient_ConnectionNotAnEntity_With_Records()
        {
            AssertResult(
                settings: new AssertSettings { EntityRecords = true },
                FileResource.Open("ChatPeopleNodes.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }
#endif

        [Fact]
        public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly()
        {
            AssertResult(
                FileResource.Open("ChatSendMessage.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }

#if !NETCOREAPP3_1
        [Fact]
        public void Generate_ChatClient_MapperMapsEntityOnRootCorrectly_With_Records()
        {
            AssertResult(
                settings: new AssertSettings { EntityRecords = true },
                FileResource.Open("ChatSendMessage.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("ChatSchema.graphql"));
        }
#endif

        [Fact]
        public void Generate_BookClient_DataOnly_UnionDataTypes()
        {
            AssertResult(
                FileResource.Open("BookUnionQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }

#if !NETCOREAPP3_1
        [Fact]
        public void Generate_BookClient_DataOnly_UnionDataTypes_With_Records()
        {
            AssertResult(
                settings: new AssertSettings { EntityRecords = true },
                FileResource.Open("BookUnionQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }
#endif

        [Fact]
        public void Generate_BookClient_DataOnly_InterfaceDataTypes()
        {
            AssertResult(
                FileResource.Open("BookInterfaceQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }

#if !NETCOREAPP3_1
        [Fact]
        public void Generate_BookClient_DataOnly_InterfaceDataTypes_With_Records()
        {
            AssertResult(
                settings: new AssertSettings { EntityRecords = true },
                FileResource.Open("BookInterfaceQuery.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }
#endif

        [Fact]
        public void Generate_BookClient_DataInEntity_UnionDataTypes()
        {
            AssertResult(
                FileResource.Open("BookUnionQueryWithEntity.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }

#if !NETCOREAPP3_1
        [Fact]
        public void Generate_BookClient_DataInEntity_UnionDataTypes_With_Records()
        {
            AssertResult(
                settings: new AssertSettings { EntityRecords = true },
                FileResource.Open("BookUnionQueryWithEntity.graphql"),
                FileResource.Open("Schema.extensions.graphql"),
                FileResource.Open("BookSchema.graphql"));
        }
#endif
    }
}
