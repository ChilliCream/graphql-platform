using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using Xunit;

namespace HotChocolate.Stitching
{
    public class SchemaInfoTests
    {
        [Fact]
        public void ResolveRootTypesWithCustomNames()
        {
            // arrange
            const string schemaName = "foo";
            DocumentNode schema = Utf8GraphQLParser.Parse(
                FileResource.Open("SchemaInfoTests_Schema.graphql"));

            // act
            var schemaInfo = new SchemaInfo(schemaName, schema);

            // assert
            Assert.NotNull(schemaInfo.QueryType);
            Assert.Equal("CustomQuery",
                schemaInfo.QueryType.Name.Value);

            Assert.NotNull(schemaInfo.MutationType);
            Assert.Equal("CustomMutation",
                schemaInfo.MutationType.Name.Value);

            Assert.NotNull(schemaInfo.SubscriptionType);
            Assert.Equal("CustomSubscription",
                schemaInfo.SubscriptionType.Name.Value);
        }

        [Fact]
        public void GetOperationTypeByRootType_Query()
        {
            // arrange
            const string schemaName = "foo";
            DocumentNode schema = Utf8GraphQLParser.Parse(
                FileResource.Open("SchemaInfoTests_Schema.graphql"));
            var schemaInfo = new SchemaInfo(schemaName, schema);

            // act
            bool success = schemaInfo.TryGetOperationType(
                schemaInfo.QueryType,
                out OperationType operationType);

            // assert
            Assert.True(success);
            Assert.Equal(OperationType.Query, operationType);
        }

        [Fact]
        public void GetOperationTypeByRootType_Mutation()
        {
            // arrange
            const string schemaName = "foo";
            DocumentNode schema = Utf8GraphQLParser.Parse(
                FileResource.Open("SchemaInfoTests_Schema.graphql"));
            var schemaInfo = new SchemaInfo(schemaName, schema);

            // act
            bool success = schemaInfo.TryGetOperationType(
                schemaInfo.MutationType,
                out OperationType operationType);

            // assert
            Assert.True(success);
            Assert.Equal(OperationType.Mutation, operationType);
        }

        [Fact]
        public void GetOperationTypeByRootType_Subscription()
        {
            // arrange
            const string schemaName = "foo";
            DocumentNode schema = Utf8GraphQLParser.Parse(
                FileResource.Open("SchemaInfoTests_Schema.graphql"));
            var schemaInfo = new SchemaInfo(schemaName, schema);

            // act
            bool success = schemaInfo.TryGetOperationType(
                schemaInfo.SubscriptionType,
                out OperationType operationType);

            // assert
            Assert.True(success);
            Assert.Equal(OperationType.Subscription, operationType);
        }
    }
}
