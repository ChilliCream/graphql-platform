using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ResolverTaskNullTests
    {
        [InlineData("case1", "abc")]
        [InlineData("case1", null)]
        [InlineData("case2", "abc")]
        [InlineData("case2", null)]
        [InlineData("case3", "abc")]
        [InlineData("case3", null)]
        [InlineData("case4", "abc")]
        [InlineData("case4", null)]
        [Theory]
        public async Task HandleNullResolverTask(string field, string argument)
        {
            // arrange
            IQueryExecutor executor =
                Schema.Create(c => c.RegisterQueryType<QueryType>())
                    .MakeExecutable();

            // act
            string arg = argument == null ? "null" : $"\"{argument}\"";
            IExecutionResult result = await executor.ExecuteAsync(
                $"{{ {field}(name: {arg}) }}");

            // assert
            result.MatchSnapshot(SnapshotNameExtension.Create(
                field, argument ?? "null"));
        }

        public class QueryType
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Field<QueryResolver>(t => t.Case2(default));
                descriptor.Field("case3")
                    .Argument("name", a => a.Type<StringType>())
                    .Type<StringType>()
                    .Resolver(new FieldResolverDelegate(ctx =>
                    {
                        string name = ctx.Argument<string>("name");
                        if (name == null)
                        {
                            return null;
                        }
                        return Task.FromResult<object>(name);
                    }));

                descriptor.Field("case4")
                    .Argument("name", a => a.Type<StringType>())
                    .Type<StringType>()
                    .Resolver(ctx =>
                    {
                        string name = ctx.Argument<string>("name");
                        if (name == null)
                        {
                            return null;
                        }
                        return Task.FromResult(name);
                    });
            }
        }

        public class Query
        {
            public Task<string> Case1(string name)
            {
                if (name == null)
                {
                    return null;
                }
                return Task.FromResult(name);
            }
        }

        public class QueryResolver
        {
            public Task<string> Case2(string name)
            {
                if (name == null)
                {
                    return null;
                }
                return Task.FromResult(name);
            }
        }
    }
}
