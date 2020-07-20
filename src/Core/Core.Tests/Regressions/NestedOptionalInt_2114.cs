using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

#nullable enable

namespace HotChocolate.Regressions
{
    // Relates to issue https://github.com/ChilliCream/hotchocolate/issues/2114
    public class NestedOptionalInt_2114
    {
        [Fact]
        public async Task ShouldNotFailWithExplicitValues()
        {
            // arrange
            IQueryExecutor executor = CreateSchema().MakeExecutable();
            const string Query = @"
                mutation {
                  eat(topping: { pickles: [{ butterPickle: { size: 5 } }] })
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(Query);

            // assert
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ShouldNotFailWithVariables()
        {
            // arrange
            IQueryExecutor executor = CreateSchema().MakeExecutable();
            const string Query = @"
                mutation a($input: ButterPickleInput!)
                {
                  eat(topping: { pickles: [{ butterPickle: $input }] })
                }";

            // act
            IExecutionResult result = await executor.ExecuteAsync(Query,
                new Dictionary<string, object> 
                { 
                    {"input", new Dictionary<string, object> { {"size", 5} } } 
                });

            // assert
            Assert.Empty(result.Errors);
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(s => s
                .RegisterQueryType<Query>()
                .RegisterMutationType<Mutation>());
        }

        public class Query
        {
            public string Chocolate => "rain";
        }

        public class Mutation
        {
            public bool Eat(ToppingInput topping)
            {
                return true;
            }

            public bool Consume(ButterPickleInput input)
            {
                return true;
            }
        }

        public class ToppingInput
        {
            public IEnumerable<PicklesInput>? Pickles { get; set; }
        }

        public class PicklesInput
        {
            public ButterPickleInput? ButterPickle { get; set; }
        }

        public class ButterPickleInput
        {
            public Optional<int> Size { get; set; }
        }
    }
}
