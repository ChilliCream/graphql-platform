using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Customers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Requests
{
    public class BufferedRequestTests
    {
        [Fact]
        public async Task Create_BufferedRequest()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            var query = "query abc($id: ID) { customer(id: $id) { name } }";

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create();

            // act
            var bufferedRequest = BufferedRequest.Create(request, schema);

            // assert
            Assert.Equal(request, bufferedRequest.Request);
            Assert.Equal(query, bufferedRequest.Document.ToString(false));
            Assert.Equal(
                bufferedRequest.Document.Definitions.OfType<OperationDefinitionNode>().First(),
                bufferedRequest.Operation);
            Assert.NotNull(bufferedRequest.Promise);
            Assert.Null(bufferedRequest.Aliases);
        }

        [Fact]
        public async Task Create_BufferedRequest_Operation_Correctly_Resolved()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            var query = "query abc($id: ID) { customer(id: $id) { name } } " +
                "query def($id: ID) { customer(id: $id) { name } }";

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetOperation("def")
                    .Create();

            // act
            var bufferedRequest = BufferedRequest.Create(request, schema);

            // assert
            Assert.Equal(request, bufferedRequest.Request);
            Assert.Equal(query, bufferedRequest.Document.ToString(false));
            Assert.Equal(
                bufferedRequest.Document.Definitions.OfType<OperationDefinitionNode>().Last(),
                bufferedRequest.Operation);
            Assert.NotNull(bufferedRequest.Promise);
            Assert.Null(bufferedRequest.Aliases);
        }

        [Fact]
        public async Task Create_BufferedRequest_Rewrite_Variables_To_Literals()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            var query = "query abc($id: ID) { customer(id: $id) { name } }";

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValue("id", 1)
                    .Create();

            // act
            var bufferedRequest = BufferedRequest.Create(request, schema);

            // assert
            Assert.NotEqual(request, bufferedRequest.Request);
            Assert.Collection(bufferedRequest.Request.VariableValues!,
                t => Assert.IsType<StringValueNode>(t.Value));
        }

        [Fact]
        public async Task Create_BufferedRequest_Rewrite_Variables_To_Literals_2()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            var query = "query abc($id: ID) { customer(id: $id) { name } }";

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValue("id", "1")
                    .Create();

            // act
            var bufferedRequest = BufferedRequest.Create(request, schema);

            // assert
            Assert.NotEqual(request, bufferedRequest.Request);
            Assert.Collection(bufferedRequest.Request.VariableValues!,
                t => Assert.IsType<StringValueNode>(t.Value));
        }

        [Fact]
        public async Task Create_BufferedRequest_Literals_Are_Not_Rewritten()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            var query = "query abc($id: ID) { customer(id: $id) { name } }";

            var idValue = new StringValueNode("1");

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .SetVariableValue("id", idValue)
                    .Create();

            // act
            var bufferedRequest = BufferedRequest.Create(request, schema);

            // assert
            Assert.NotEqual(request, bufferedRequest.Request);
            Assert.Collection(bufferedRequest.Request.VariableValues!,
                t => Assert.Same(idValue, t.Value));
        }

        [Fact]
        public async Task Create_BufferedRequest_Request_Is_Null()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            // act
            void Action() => BufferedRequest.Create(null!, schema);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public async Task Create_BufferedRequest_Request_Query_Is_Null()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddCustomerSchema()
                    .BuildSchemaAsync();

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQueryId("abc")
                    .Create();

            // act
            void Action() => BufferedRequest.Create(request, schema);

            // assert
            Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void Create_BufferedRequest_Schema_Is_Null()
        {
            // arrange
            var query = "query abc($id: ID) { customer(id: $id) { name } }";

            IQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(query)
                    .Create();

            // act
            void Action() => BufferedRequest.Create(request, null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
