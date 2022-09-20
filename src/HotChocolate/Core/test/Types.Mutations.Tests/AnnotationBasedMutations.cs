using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

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
    public async Task SimpleMutationReturnList_Inferred()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationReturnList>()
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
    public async Task SimpleMutationReturnList_Inferred_Execute()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<SimpleMutationReturnList>()
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
    public async Task Ensure_That_Directive_Middleware_Play_Nice()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType()
            .AddTypeExtension<SimpleMutationExtension>()
            .AddDirectiveType(new DirectiveType(d =>
            {
                d.Name("foo");
                d.Location(DirectiveLocation.Field);
                d.Use(next => async context =>
                {
                    // this is just a dummy middleware
                    await next(context);
                });
            }))
            .AddMutationConventions(
                new MutationConventionOptions
                {
                    ApplyToAllMutations = true
                })
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: { something: ""abc"" }) @foo {
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
            .AddMutationConventions(false)
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

    [Fact]
    public async Task Allow_Payload_Result_Field_To_Be_Null()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithInputPayload>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: { userId: 1 }) {
                        user { name }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Allow_Id_Middleware()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithIds>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: {
                        id: ""Rm9vCmdhYWY1ZjAzNjk0OGU0NDRkYWRhNTM2ZTY1MTNkNTJjZA==""
                    }) {
                        user { name id }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Allow_InputObject_Middleware()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithInputObject>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: {
                        test: {
                            name: ""foo""
                        }
                    }) {
                        user { name }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Union_Result_1()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithUnionResult1>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: {
                        something: ""abc""
                    }) {
                        errors { ... on Custom2Error { message } }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Union_Result_1_Schema()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithUnionResult1>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Union_Result_2()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithUnionResult2>()
            .AddMutationConventions(true)
            .ModifyOptions(o => o.StrictValidation = false)
            .ExecuteRequestAsync(
                @"mutation {
                    doSomething(input: {
                        something: ""abc""
                    }) {
                        errors { ... on Custom2Error { message } }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Union_Result_2_Schema()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithUnionResult2>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class SimpleMutation
    {
        public string DoSomething(string something)
            => something;
    }

    public class SimpleMutationReturnList
    {
        public System.Collections.Generic.List<string> DoSomething(string something) => new() { something };
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
        public string MyResult1 { get; set; } = default!;

        public string MyResult2 { get; set; } = default!;
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
        public string MyInput1 { get; set; } = default!;

        public string MyInput2 { get; set; } = default!;
    }

    public class MutationWithIds
    {
        public User? DoSomething([ID("Foo")]Guid  id)
        {
            return new User() {Name = "Foo", Id = id,};
        }
    }

    public class MutationWithInputObject
    {
        public User? DoSomething(Test  test)
        {
            return new User() {Name = test.Name};
        }
    }

    public class Test
    {
        public string Name { get; set; } = default!;

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

    public class MutationWithUnionResult1
    {
        [Error(typeof(CustomException))]
        [Error(typeof(Custom2Exception))]
        public MutationResult<string> DoSomething(string something)
            => new(new Custom2Exception());
    }

    public class MutationWithUnionResult2
    {
        public MutationResult<string, Custom2Exception> DoSomething(string something)
            => new Custom2Exception();
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

    public class MutationWithInputPayload
    {
        public User? DoSomething(int userId)
        {
            return null;
        }
    }

    public class User
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }
    }
}
