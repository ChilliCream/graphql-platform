using HotChocolate.Configuration.Validation;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class OneOfIntegrationTests : TypeValidationTestBase
{
    [Fact]
    public async Task A_is_set_and_B_is_set_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: "abc", b: 123 })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_null_and_B_is_set_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: null, b: 123 })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_null_and_B_is_null_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: null, b: null })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_null_Error()
    {
        // Error: Value for member field {a} must be non-null
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: null })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_Valid()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { b: 123 })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Input_is_empty_object_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_variable_and_B_is_set_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($a: String!) {
                            example(input: { a: $a, b: 123 })
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "a": null
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_unset_variable_and_B_is_set_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($a: String!) {
                            example(input: { a: $a, b: 123 })
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_variable_and_B_is_unset_variable_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($a: String!, $b: Int!) {
                            example(input: { a: $a, b: $b })
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "a": "abc"
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_is_123_Valid()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($b: Int!) {
                            example(input: { b: $b })
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "b": 123
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_field_B_set_to_123_Valid()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "b": 123 }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_A_set_to_abc_and_B_set_to_123_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "a": "abc", "b": 123 }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_A_set_to_abc_and_B_set_to_null_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "a": "abc", "b": null }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_A_set_to_null_Error()
    {
        // Error: Value for member field {a} must be non-null
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                          example(input: $var)
                        }
                        """
                        )
                    .SetVariableValues(
                        """
                        {
                          "var": { "a": null }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_empty_object_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                          example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                          "var": { }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Input_is_set_to_string_abc123_Error()
    {
        // Error: Incorrect value
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            example(input: "abc123")
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_string_abc123_and_passed_to_input_Error()
    {
        // Error: Incorrect value
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: String!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": "abc123"
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_and_B_is_set_to_string_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: "abc", b: "123" })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_to_string_Error()
    {
        // Error: Incorrect value for member field {b}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { b: "123" })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_B_set_to_abc_Error()
    {
        // Error: Incorrect value for member field {b}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "b": "abc" }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_to_string_Valid()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: "abc" })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_not_set_Error()
    {
        // Error: Value for member field {b} must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($b: Int!) {
                            example(input: { b: $b })
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_field_A_set_to_abc_Valid()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "a": "abc" }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_and_B_is_null_Error()
    {
        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { a: "abc", b: null })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_is_null_Error()
    {
        // Error: Value for member field {b} must be non-null
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($b: Int) {
                            example(input: { b: $b })
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "b": null
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_and_C_is_invalid_prop_Error()
    {
        // Error: Unexpected field {c}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                """
                {
                    example(input: { b: 123, c: "xyz" })
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_fields_B_and_C_set_Error()
    {
        // Error: Unexpected field {c}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($var: ExampleInput!) {
                            example(input: $var)
                        }
                        """)
                    .SetVariableValues(
                        """
                        {
                            "var": { "b": 123, "c": "xyz" }
                        }
                        """)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public void OneOf_Input_Objects_that_is_Valid()
        => ExpectValid(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String
                b: Int
            }");

    [Fact]
    public void OneOf_Input_Objects_must_have_nullable_fields()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String!
                b: Int
            }");

    [Fact]
    public void OneOf_Input_Objects_must_have_nullable_fields_with_two_fields_non_null()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String!
                b: Int!
            }");

    [Fact]
    public void OneOf_Input_Objects_must_have_nullable_fields_with_one_field_has_default()
        => ExpectError(
            """
            type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String = "a"
                b: Int
            }
            """);

    [Fact]
    public void OneOf_Input_Objects_must_have_nullable_fields_with_two_fields_that_have_default()
        => ExpectError(
            """
            type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String = "a"
                b: Int = 1
            }
            """);

    [Fact]
    public void OneOf_generic_code_first_schema()
        => SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .Create()
            .ToString()
            .MatchSnapshot();

    [Fact]
    public async Task OneOf_introspection()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(
                o =>
                {
                    o.EnableOneOf = true;
                    o.StrictValidation = true;
                })
            .ExecuteRequestAsync(
                """
                {
                    oneOf_input: __type(name: "ExampleInput") {
                        # should be true
                        isOneOf
                    }

                    input: __type(name: "StandardInput") {
                        # should be false
                        isOneOf
                    }

                    object: __type(name: "Query") {
                        # should be null
                        isOneOf
                    }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task OneOf_DefaultValue_On_Directive_Argument()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddType<DefaultValue>()
            .AddDocumentFromString(
                """
                type Query {
                    foo: String @defaultValue(value: { string: "abc" })
                }
                """)
            .AddResolver("Query", "foo", "abc")
            .ModifyOptions(o => o.EnableOneOf = true)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task OneOf_DefaultValue_On_Directive_Argument_Fluent()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddType<DefaultValueDirectiveType>()
            .AddType<DefaultValueType>()
            .AddDocumentFromString(
                """
                type Query {
                    foo: String @defaultValue(value: { string: "abc" })
                }
                """)
            .AddResolver("Query", "foo", "abc")
            .ModifyOptions(o => o.EnableOneOf = true)
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public string Example(ExampleInput input)
        {
            if (input.A is not null)
            {
                return "a: " + input.A;
            }

            return "b: " + input.B;
        }

        public string Standard(StandardInput input)
            => "abc";
    }

    [OneOf]
    public class ExampleInput
    {
        public string? A { get; set; }

        public int? B { get; set; }
    }

    public class StandardInput
    {
        public string? Foo { get; set; }
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Name("Query")
                .Field("a")
                .Argument("a", a => a.Type<Example2InputType>())
                .Type<StringType>()
                .Resolve("abc");
        }
    }

    public class Example2InputType : InputObjectType<Example2Input>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Example2Input> descriptor)
        {
            descriptor.OneOf();
        }
    }

    public class Example2Input
    {
        public string? A { get; set; }

        public int? B { get; set; }
    }

    [DirectiveType(DirectiveLocation.FieldDefinition)]
    public class DefaultValue
    {
        public DefaultValueInput? Value { get; set; }
    }

    [OneOf]
    public class DefaultValueInput
    {
        public string? String { get; set; }

        public int? Int { get; set; }
    }

    public class DefaultValueType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("DefaultValue");
            descriptor.OneOf();
            descriptor.Field("string").Type<StringType>();
            descriptor.Field("int").Type<IntType>();
        }
    }

    public class DefaultValueDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("defaultValue");
            descriptor.Argument("value").Type<DefaultValueType>();
            descriptor.Location(DirectiveLocation.FieldDefinition);
        }
    }
}
