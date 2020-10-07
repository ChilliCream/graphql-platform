using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Customers;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Requests
{
    public class MergeRequestHelperTests
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

            var queryA = "query abc($id: ID) { customer(id: $id) { name } }";
            var queryB = "query abc($id: ID) { customer(id: $id) { id } }";

            IQueryRequest requestA =
                QueryRequestBuilder.New()
                    .SetQuery(queryA)
                    .SetVariableValue("id", "1")
                    .Create();

            IQueryRequest requestB =
                QueryRequestBuilder.New()
                    .SetQuery(queryB)
                    .SetVariableValue("id", "1")
                    .Create();

            var bufferedRequestA = BufferedRequest.Create(requestA, schema);
            var bufferedRequestB = BufferedRequest.Create(requestB, schema);

            // act
            IEnumerable<(IQueryRequest, IEnumerable<BufferedRequest>)> mergeResult =
                MergeRequestHelper.MergeRequests(new[] { bufferedRequestA, bufferedRequestB });

            // assert
            string.Join(Environment.NewLine + "-------" + Environment.NewLine,
                mergeResult
                    .Select(t => t.Item1)
                    .Select(t => Utf8GraphQLParser.Parse(t.Query!.AsSpan()).ToString(true)))
                .MatchSnapshot();
        }
    }
}
