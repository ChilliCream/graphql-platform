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
    public async Task SimpleMutation_Inferred_With_QueryField()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("abc").Resolve("def"))
            .AddMutationType<SimpleMutation>()
            .AddMutationConventions(
                new MutationConventionOptions
                {
                    ApplyToAllMutations = true
                })
            .AddQueryFieldToMutationPayloads()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutationExtension_Inferred()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType()
            .AddTypeExtension<SimpleMutationExtension>()
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
    public async Task SimpleMutationExtension_Inferred_Execute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType()
            .AddTypeExtension<SimpleMutationExtension>()
            .AddMutationConventions(
                new MutationConventionOptions
                {
                    ApplyToAllMutations = true
                })
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: { something: ""abc"" }) {
                        string
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_MutationAttributeOnQuery()
    {
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithMutationAnnotation>()
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions(true)
                .BuildSchemaAsync();

        await Assert.ThrowsAsync<SchemaException>(Error);
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Execute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutation>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: { something: ""abc"" }) {
                        string
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Single_Error()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationWithSingleError>()
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
    public async Task SimpleMutation_Inferred_With_Single_Error_Execute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationWithSingleError>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: { something: ""abc"" }) {
                        string
                        errors {
                            ... on CustomError {
                                message
                            }
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Two_Errors()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationWithTwoErrors>()
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
    public async Task SimpleMutation_Inferred_Defaults()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationWithSingleError>()
            .AddMutationConventions(
                new MutationConventionOptions
                {
                    InputArgumentName = "inputArgument",
                    InputTypeNamePattern = "{MutationName}In",
                    PayloadTypeNamePattern = "{MutationName}Out",
                    PayloadErrorTypeNamePattern = "{MutationName}Fault",
                    ApplyToAllMutations = true
                })
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

    [Fact]
    public async Task SimpleMutation_Attribute_OptOut()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationAttributeOptOut>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Override_Payload()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationPayloadOverride>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SimpleMutation_Override_Input()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationInputOverride>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MultipleArgumentMutation_Inferred()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MultipleArgumentMutation>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class SimpleMutation
    {
        public string DoSomething(string something)
            => something;
    }

    [ExtendObjectType("Mutation")]
    public class SimpleMutationExtension
    {
        public string DoSomething(string something)
            => something;
    }

    public class SimpleMutationAttribute
    {
        [UseMutationConvention(
            InputTypeName = "InputTypeName",
            InputArgumentName = "inputArgumentName",
            PayloadTypeName = "PayloadTypeName",
            PayloadFieldName = "payloadFieldName")]
        public string DoSomething(string something)
        {
            throw new Exception();
        }
    }

    public class SimpleMutationAttributeOptOut
    {
        [UseMutationConvention(Disable = true)]
        public string DoSomething(string something)
        {
            throw new Exception();
        }
    }

    public class SimpleMutationPayloadOverride
    {
        public DoSomethingPayload DoSomething(string something)
        {
            throw new Exception();
        }
    }

    public class DoSomethingPayload
    {
        public string MyResult1 { get; set; }

        public string MyResult2 { get; set; }
    }

    public class SimpleMutationInputOverride
    {
        public string DoSomething(DoSomethingInput something)
        {
            throw new Exception();
        }
    }

    public class DoSomethingInput
    {
        public string MyInput1 { get; set; }

        public string MyInput2 { get; set; }
    }

    public class MultipleArgumentMutation
    {
        public string DoSomething(string something1, string something2)
        {
            throw new Exception();
        }
    }

    public class SimpleMutationWithSingleError
    {
        [Error(typeof(CustomException))]
        public string DoSomething(string something)
            => throw new CustomException();
    }

    public class SimpleMutationWithTwoErrors
    {
        [Error(typeof(CustomException))]
        [Error(typeof(Custom2Exception))]
        public string DoSomething(string something)
            => throw new CustomException();
    }

    public class CustomException : Exception
    {
        public override string Message => "Hello";
    }

    public class Custom2Exception : Exception
    {
        public override string Message => "Hello2";
    }

    public class QueryWithMutationAnnotation
    {
        [UseMutationConvention]
        public string GetFoo() => "foo";
    }
}
