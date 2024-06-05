using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using SnapshotExtensions = CookieCrumble.SnapshotExtensions;

namespace HotChocolate.Types;

public class AnnotationBasedMutations
{
    [Fact]
    public async Task SimpleMutation_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_ErrorObj()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationWithErrorObj>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Global_Errors()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions()
                .AddMutationErrorConfiguration<CustomErrorConfig>()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Global_Errors_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions()
                .AddMutationErrorConfiguration<CustomErrorConfig>()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                      doSomething(input: { something: "abc" }) {
                        string
                        errors {
                            __typename
                            ... on Error {
                                message
                            }
                        }
                      }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Query_Field_Stays_NonNull()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions()
                .AddQueryFieldToMutationPayloads()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutationReturnList_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationReturnList>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutationReturnList_Inferred_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationReturnList>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                      doSomething(input: { something: "abc" }) {
                        string
                      }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_QueryField()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .AddQueryFieldToMutationPayloads()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutationExtension_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<SimpleMutationExtension>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutationExtension_Inferred_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<SimpleMutationExtension>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { something: "abc" }) {
                            string
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task SimpleJsonMutationExtension_Inferred_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<SimpleJsonMutationExtension>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { something: 10 }) {
                            string
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Ensure_That_Directive_Middleware_Play_Nice()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<SimpleMutationExtension>()
                .AddDirectiveType(
                    new DirectiveType(
                        d =>
                        {
                            d.Name("foo");
                            d.Location(DirectiveLocation.Field);
                            d.Use(
                                (next, _) => async context =>
                                {
                                    // this is just a dummy middleware
                                    await next(context);
                                });
                        }))
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { something: "abc" }) @foo {
                            string
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
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
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutation>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                      doSomething(input: { something: "abc" }) {
                        string
                      }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Single_Error()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationWithSingleError>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Single_Error_Query_Field_Stays_Non_Null()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<SimpleMutationWithSingleError>()
                .AddMutationConventions()
                .AddQueryFieldToMutationPayloads()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Single_Error_Execute()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationWithSingleError>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { something: "abc" }) {
                            string
                            errors {
                                ... on CustomError {
                                    message
                                }
                            }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task SimpleMutation_Inferred_With_Two_Errors()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationWithTwoErrors>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Defaults()
    {
        var schema =
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
                        ApplyToAllMutations = true,
                    })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Inferred_Attribute()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationAttribute>()
                .AddMutationConventions(
                    new MutationConventionOptions { ApplyToAllMutations = true, })
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Attribute()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationAttribute>()
                .AddMutationConventions(false)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Attribute_OptOut()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationAttributeOptOut>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Override_Payload()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationPayloadOverride>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Override_Input()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationInputOverride>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SimpleMutation_Add_Error_Via_Type_Interceptor()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<SimpleMutationInputOverride>()
                .TryAddTypeInterceptor<SimpleMutation_ErrorViaTypeInterceptor>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task MultipleArgumentMutation_Inferred()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MultipleArgumentMutation>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Allow_Payload_Result_Field_To_Be_Null()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithInputPayload>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { userId: 1 }) {
                            user { name }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Allow_Id_Middleware()
    {
        var id = TestHelper.EncodeId("Foo", new Guid("aaf5f036-948e-444d-ada5-36e6513d52cd"));

        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithIds>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .AddGlobalObjectIdentification()
                .ExecuteRequestAsync(
                    OperationRequestBuilder
                        .Create()
                        .SetDocument(
                            """
                            mutation($id: ID!) {
                                doSomething(input: {
                                    id: $id
                                }) {
                                    user { name id }
                                }
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?> { { "id", id } })
                        .Build());

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Allow_Id_Middleware2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithIds2>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .AddGlobalObjectIdentification()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            id: "Rm9vCmdhYWY1ZjAzNjk0OGU0NDRkYWRhNTM2ZTY1MTNkNTJjZA=="
                        }) {
                            user { name id }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Allow_InputObject_Middleware()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithInputObject>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            test: {
                                name: "foo"
                            }
                        }) {
                            user { name }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult1>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_1_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult1>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult2>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_2_Task()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<MutationWithUnionResult2_Task>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_2_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult2_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_2_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult2>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_2_Task_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType()
                .AddTypeExtension<MutationWithUnionResult2_Task>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_2_Error_Name_Collision()
    {
        async Task Error() => await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType<MutationWithErrorCollision>()
            .AddMutationConventions()
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_3()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult3>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_3_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult3_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_3_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult3>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_4()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult4>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_4_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult4_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_4_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult4>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_5()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult5>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_5_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult5_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);
        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_5_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult5>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_6()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult6>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_6_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult6_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_6_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult6>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Union_Result_7()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult7>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_7_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult7_Success>()
                .AddMutationConventions(true)
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: {
                            something: "abc"
                        }) {
                            string
                            errors { ... on Error { message } }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Union_Result_7_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithUnionResult7>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Payload_Override_With_Errors()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithPayloadOverride>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Payload_Override_With_Errors_Execution_On_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithPayloadOverride>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething2(input: { userId: 1 }) {
                            userId
                            errors {
                                __typename
                            }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Payload_Override_With_Errors_Execution_On_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationWithPayloadOverride>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething2(input: { userId: null }) {
                            userId
                            errors {
                                __typename
                            }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Query_Filed_Stays_NonNull()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationWithPayloadOverride>()
                .AddMutationConventions()
                .AddQueryFieldToMutationPayloads()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task List_Return_Type()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<ListReturnMutation>()
                .AddMutationConventions()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_Aggregate_Error_Not_Mapped()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationAggregateError>()
                .AddMutationConventions()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething2(input: { userId: 1 }) {
                            userId
                            errors {
                                __typename
                            }
                        }
                    }
                    """);

        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Mutation_With_Optional_Arg()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationWithOptionalArg>()
                .AddMutationConventions()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething(input: { }) {
                            string
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "doSomething": {
                  "string": "nothing"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Mutation_With_ErrorWithInterface()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationWithInterfaces>()
                .AddType<IInterfaceError>()
                .AddType<IInterfaceError2>()
                .AddMutationConventions()
                .BuildSchemaAsync();

        result.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_With_ErrorAnnotatedAndCustomInterface()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationWithErrorInterface>()
                .AddErrorInterfaceType<IErrorInterface>()
                .AddType<IInterfaceError>()
                .AddType<IInterfaceError2>()
                .AddMutationConventions()
                .BuildSchemaAsync();

        result.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Mutation_With_MutationConventionsAndNamingConventions()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddMutationType<MutationConventionsAndNamingConventionsMutation>()
                .AddConvention<INamingConventions, CustomNamingConvention>()
                .AddMutationConventions()
                .ModifyOptions(o => o.StrictValidation = false)
                .ExecuteRequestAsync(
                    """
                    mutation {
                        doSomething_Named(input: { name_Named: "coco" }) {
                            user_Named {
                                id_Named
                                name_Named
                            }
                        }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "doSomething_Named": {
                  "user_Named": {
                    "id_Named": "00000000-0000-0000-0000-000000000000",
                    "name_Named": "coco"
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Mutation_With_ErrorAnnotatedAndCustomInterface_LateAndEarlyRegistration()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("abc").Resolve("def"))
                .AddMutationType<MutationWithErrorInterface2>()
                .AddErrorInterfaceType<IErrorInterface>()
                .AddType<IInterfaceError>()
                .AddType<IInterfaceError2>()
                .AddMutationConventions()
                .BuildSchemaAsync();

        result.Print().MatchSnapshot();
    }

    public class SimpleMutation
    {
        public string DoSomething(string something)
            => something;
    }

    public class SimpleMutationReturnList
    {
        public System.Collections.Generic.List<string> DoSomething(string something)
            => [something,];
    }

    [ExtendObjectType("Mutation")]
    public class SimpleMutationExtension
    {
        public string DoSomething(string something)
            => something;
    }

    [ExtendObjectType("Mutation")]
    public class SimpleJsonMutationExtension
    {
        public string DoSomething(System.Text.Json.JsonElement something)
            => "Done";
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

    internal class SimpleMutation_ErrorViaTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase definition)
        {
            if (definition is not ObjectTypeDefinition objTypeDef)
            {
                return;
            }
        }

        public override void OnBeforeRegisterDependencies(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objTypeDef)
            {
                foreach (var fieldDef in objTypeDef.Fields)
                {
                    if (fieldDef.Name != "doSomething")
                    {
                        continue;
                    }

                    foreach (var argDef in fieldDef.Arguments)
                    {
                        if (argDef.Name == "something")
                        {
                            fieldDef.AddErrorType(
                                discoveryContext.DescriptorContext,
                                typeof(OutOfMemoryException));
                        }
                    }
                }
            }
        }
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
        public User? DoSomething([ID("Foo")] Guid id)
        {
            return new User { Name = "Foo", Id = id, };
        }
    }

    public class MutationWithIds2
    {
        public User? DoSomething([ID<Foo>] Guid id)
        {
            return new User { Name = "Foo", Id = id, };
        }
    }

    public class Foo
    {
    }

    public class MutationWithInputObject
    {
        public User? DoSomething(Test test)
        {
            return new User { Name = test.Name, };
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

    public class MutationConventionsAndNamingConventionsMutation
    {
        public User DoSomething(string name)
        {
            return new User { Name = name, };
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
        public FieldResult<string> DoSomething(string something)
            => new(new Custom2Exception());
    }

    public class MutationWithUnionResult2
    {
        public FieldResult<string, Custom2Exception> DoSomething(string something)
            => new Custom2Exception();
    }

    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class MutationWithUnionResult2_Task
    {
        public async Task<FieldResult<string, Custom2Exception>> DoSomething(string something)
            => await Task.FromResult(new Custom2Exception());
    }

    public class MutationWithUnionResult2_Success
    {
        public FieldResult<string, Custom2Exception> DoSomething(string something)
            => something;
    }

    public class MutationWithUnionResult3
    {
        public FieldResult<string, CustomException, Custom2Exception> DoSomething(
            string something)
            => new Custom2Exception();
    }

    public class MutationWithUnionResult3_Success
    {
        public FieldResult<string, CustomException, Custom2Exception> DoSomething(
            string something)
            => something;
    }

    public class MutationWithUnionResult4
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom3Exception>
            DoSomething(string something)
            => new Custom2Exception();
    }

    public class MutationWithUnionResult4_Success
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom3Exception>
            DoSomething(string something)
            => something;
    }

    public class MutationWithUnionResult5
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception>
            DoSomething(string something)
            => new Custom4Exception();
    }

    public class MutationWithUnionResult5_Success
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception>
            DoSomething(string something)
            => something;
    }

    public class MutationWithUnionResult6
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception, Custom5>
            DoSomething(string something)
            => new Custom5();
    }

    public class MutationWithUnionResult6_Success
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception, Custom5>
            DoSomething(string something)
            => something;
    }

    public class MutationWithUnionResult7
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception, Custom5,
            Custom6> DoSomething(string something)
            => new Custom5();
    }

    public class MutationWithUnionResult7_Success
    {
        public FieldResult<string, CustomException, Custom2Exception, Custom4Exception, Custom5,
            Custom6> DoSomething(string something)
            => something;
    }

    public class CustomException : Exception
    {
        public override string Message => "Hello";
    }

    public class Custom2Exception : Exception
    {
        public override string Message => "Hello2";
    }

    public class Custom3Exception : Exception
    {
        public override string Message => "Hello3";
    }

    public class Custom4Exception : Exception
    {
        public override string Message => "Hello4";
    }

    public class Custom5
    {
        public string Message => "Hello5";
    }

    public class Custom6
    {
        public string Message => "Hello6";
    }

    public class Custom7
    {
        public Custom7(Custom6 c)
        {
            Message = c.Message;
        }

        public string Message { get; }
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

    public class MutationWithErrorCollision
    {
        public FieldResult<string, FooError> Foo()
            => new FooError("some error");
    }

    public class FooError
    {
        public FooError(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class MutationWithPayloadOverride
    {
        [Error<CustomException>]
        [Error<Custom2Exception>]
        public DoSomething2Payload DoSomething2(int? userId)
            => userId.HasValue
                ? new DoSomething2Payload(userId)
                : throw new CustomException();
    }

    public class MutationAggregateError
    {
        [Error<CustomException>]
        public DoSomething2Payload DoSomething2(int? userId)
        {
            var errors = new List<Exception>();
            errors.Add(new CustomException());
            errors.Add(new Custom2Exception());
            throw new AggregateException(errors);
        }
    }

    public class SimpleMutationWithErrorObj
    {
        [Error<SomeNewError>]
        public string DoSomething(string something)
            => something;
    }

    public record DoSomething2Payload(int? UserId);

    public class ListReturnMutation
    {
        public FieldResult<List<ResultItem>> AddItem(AddItemInput input)
            => new List<ResultItem>
            {
                new(),
                new(),
                new(),
            };

        public class AddItemInput
        {
            public int Count { get; set; }
        }

        public class ResultItem
        {
            public string Name { get; set; } = "Test";
        }
    }

    public record SomeNewError(string Message);

    public class CustomErrorConfig : MutationErrorConfiguration
    {
        public override void OnConfigure(
            IDescriptorContext context,
            ObjectFieldDefinition mutationField)
        {
            mutationField.AddErrorType(context, typeof(SomeNewError));
            mutationField.MiddlewareDefinitions.Add(
                new(next => async ctx =>
                {
                    await next(ctx);
                    ctx.Result = new FieldError(new SomeNewError("This is my error."));
                }));
        }
    }

    public class MutationWithOptionalArg
    {
        public string DoSomething(Optional<string?> something)
            => something.Value ?? "nothing";
    }

    public class MutationWithInterfaces
    {
        [Error<ErrorWithInterface>]
        public bool DoSomething(string something) => true;
    }

    public class MutationWithErrorInterface
    {
        [Error<ErrorAnnotated>]
        [Error<ErrorAnnotatedAndNot>]
        public bool Annotated(string something) => true;
    }

    public class MutationWithErrorInterface2
    {
        [Error<ErrorAnnotated>]
        [Error<ErrorAnnotatedAndNot>]
        public bool Annotated(string something) => true;

        public ExampleResult ExampleResult(string something) => default!;
    }

    public class ExampleResult
    {
        public ErrorNotAnnotated NotAnnotated(string something) => default!;

        public ErrorAnnotatedAndNot Both(string something) => default!;

    }

    public class ErrorNotAnnotated : IErrorInterface
    {
        /// <inheritdoc />
        public string Message => string.Empty;
    }

    public class ErrorAnnotated : IErrorInterface, IInterfaceError
    {
        /// <inheritdoc />
        public string Message => string.Empty;

        /// <inheritdoc />
        public string Name => string.Empty;
    }

    public class ErrorAnnotatedAndNot : IErrorInterface, IInterfaceError2
    {
        /// <inheritdoc />
        public string Message => string.Empty;

        /// <inheritdoc />
        public string Name => string.Empty;
    }

    public interface IInterfaceError
    {
        public string Name { get; }
    }

    public interface IInterfaceError2
    {
        public string Name { get; }
    }

    public class ErrorWithInterface : IInterfaceError, IInterfaceError2
    {
        public string Name { get; set; } = default!;

        public string Message { get; set; } = default!;
    }

    public interface IErrorInterface
    {
        public string Message { get; }
    }

    public class CustomNamingConvention : DefaultNamingConventions
    {
        public override string GetArgumentName(ParameterInfo parameter)
        {
            var name = base.GetArgumentName(parameter);
            return name + "_Named";
        }

        public override string GetArgumentDescription(ParameterInfo parameter)
        {
            return "GetArgumentDescription";
        }

        public override string GetMemberDescription(MemberInfo member, MemberKind kind)
        {
            return "GetMemberDescription";
        }

        public override string GetTypeName(Type type, TypeKind kind)
        {
            var name = base.GetTypeName(type, kind);
            return name + "_Named";
        }

        public override string GetEnumValueDescription(object value)
        {
            return "GetEnumValueDescription";
        }

        public override string GetMemberName(MemberInfo member, MemberKind kind)
        {
            var name = base.GetMemberName(member, kind);
            return name + "_Named";
        }

        public override string GetTypeDescription(Type type, TypeKind kind)
        {
            return "GetTypeDescription";
        }
    }
}
