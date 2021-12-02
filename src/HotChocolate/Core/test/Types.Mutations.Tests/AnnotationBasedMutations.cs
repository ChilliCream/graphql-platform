using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

public class AnnotationBasedMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutation>()
            .AddMutationConventions(new() {ApplyToAllMutations = true})
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Attribute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationAttribute>()
            .AddMutationConventions(new() {ApplyToAllMutations = true})
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Attribute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationAttribute>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class SimpleMutation
    {
        public string DoSomething(string something)
        {
            throw new Exception();
        }
    }

    public class SimpleMutationAttribute
    {
        [Mutation(
            InputTypeName = "InputTypeName",
            InputArgumentName = "inputArgumentName",
            PayloadTypeName = "PayloadTypeName",
            PayloadFieldName = "payloadFieldName")]
        public string DoSomething(string something)
        {
            throw new Exception();
        }
    }

    public class SimpleMutationAttributeExplicit
    {
        [Mutation(
            Enabled = true,
            InputTypeName = "InputTypeName",
            InputArgumentName = "inputArgumentName",
            PayloadTypeName = "PayloadTypeName",
            PayloadFieldName = "payloadFieldName")]
        public string DoSomething(string something)
        {
            throw new Exception();
        }
    }
}
