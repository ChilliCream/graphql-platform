using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

namespace HotChocolate.Types;

public class CodeFirstMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType(d =>
            {
                d.Name("Mutation");
                d.Field("doSomething")
                    .Argument("a", a => a.Type<StringType>())
                    .Type<StringType>()
                    .Resolve("Abc");
            })
            .AddMutationConventions(
                new MutationConventionOptions
                {
                    ApplyToAllMutations = true
                })
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Execute()
    {
        Snapshot.FullName();

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
                    ApplyToAllMutations = true
                })
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync("mutation { doSomething(a: \"abc\") { string } }")
            .MatchSnapshotAsync();
    }
}
