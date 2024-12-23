using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Resolvers;

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
        var executor = SchemaBuilder.New()
            .AddType<QueryType>()
            .Create()
            .MakeExecutable();

        // act
        var arg = argument is null ? "null" : $"\"{argument}\"";
        var result = await executor.ExecuteAsync(
            $"{{ {field}(name: {arg}) }}");

        // assert
        result.ToJson().MatchSnapshot(postFix: $"{field}_{argument ?? "null"}");
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field<QueryResolver>(t => t.Case2(default));
            descriptor.Field("case3")
                .Argument("name", a => a.Type<StringType>())
                .Type<StringType>()
                .Resolve(ctx =>
                {
                    var name = ctx.ArgumentValue<string>("name");
                    return name is null
                        ? new ValueTask<object>(default(object))
                        : new ValueTask<object>(name);
                });

            descriptor.Field("case4")
                .Argument("name", a => a.Type<StringType>())
                .Type<StringType>()
                .Resolve(ctx =>
                {
                    var name = ctx.ArgumentValue<string>("name");
                    return name is null ? null : Task.FromResult(name);
                });
        }
    }

    public class Query
    {
        public Task<string> Case1(string name)
        {
            if (name is null)
            {
                return null!;
            }
            return Task.FromResult(name);
        }
    }

    public class QueryResolver
    {
        public Task<string> Case2(string name)
        {
            if (name is null)
            {
                return null!;
            }
            return Task.FromResult(name);
        }
    }
}
