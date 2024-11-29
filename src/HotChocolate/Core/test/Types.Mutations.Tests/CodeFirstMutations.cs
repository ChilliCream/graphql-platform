using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class CodeFirstMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType(
                    d =>
                    {
                        d.Name("Mutation");
                        d.Field("doSomething")
                            .Argument("a", a => a.Type<StringType>())
                            .Type<StringType>()
                            .Resolve("Abc");
                    })
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType(d =>
                {
                    d.Name("Mutation");
                    d.Field("doSomething")
                        .Argument("a", a => a.Type<StringType>())
                        .Type<StringType>()
                        .Resolve(ctx => ctx.ArgumentValue<string?>("a"));
                })
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync("mutation { doSomething(input: { a: \"abc\" }) { string } }");

        result.MatchSnapshot();
    }
}
