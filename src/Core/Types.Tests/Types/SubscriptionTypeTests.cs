using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class SubscriptionTypeTests
        : TypeTestBase
    {
        [Fact]
        public async Task Subscribe_With_Enumerable()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => new List<string> { "a", "b", "c" }))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync("subscription { test }");

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_Enumerable_Async()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => Task.FromResult<IEnumerable<string>>(
                        new List<string> { "a", "b", "c" })))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync("subscription { test }");

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

    }
}
