using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using CookieCrumble;

namespace HotChocolate.Types;

public class SchemaFirstMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_FieldOverride()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(@"
                    type Mutation {
                        doSomething(something: String) : String
                            @mutationConvention(payloadFieldName: ""something"")
                    }")
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions
                    {
                        ApplyToAllMutations = true
                    })
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
                .AddDocumentFromString(
                    @"
                    type Mutation {
                        doSomething(something: String) : String
                    }")
                .AddResolver(
                    "Mutation",
                    "doSomething",
                    ctx => ctx.ArgumentValue<string?>("something"))
                .BindRuntimeType<Mutation>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    @"mutation {
                        doSomething(input: { something: ""abc"" }) {
                            string
                        }
                    }");

        result.MatchSnapshot();
    }

    public class Mutation
    {
        public string? DoSomething(string? something)
            => something;
    }
}
