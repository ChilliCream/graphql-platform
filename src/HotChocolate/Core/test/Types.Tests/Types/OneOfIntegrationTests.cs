using System.Threading.Tasks;
using HotChocolate.Configuration.Validation;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

#nullable enable

public class OneOfIntegrationTests : TypeValidationTestBase
{
    [Fact]
    public async Task A_is_set_and_B_is_set_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { a: \"abc\", b: 123 }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_null_and_B_is_set_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { a: null, b: 123 }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_Valid()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { b: 123 }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_variable_and_B_is_set_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: String!) { example(input: { a: $var, b: 123 }) }")
                    .SetVariableValue("var", null)
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_is_123_Valid()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: Int!) { example(input: { b: $var }) }")
                    .SetVariableValue("var", 123)
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_field_b_set_to_123_Valid()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: ExampleInput!) { example(input: $var) }")
                    .SetVariableValue("var", new ObjectValueNode(new ObjectFieldNode("b", 123)))
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Input_is_set_to_string_abc123_Error()
    {
        Snapshot.FullName();

        // Error: Incorrect value

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ example(input: \"abc123\") }")
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_string_abc123_and_passed_to_input_Error()
    {
        Snapshot.FullName();

        // Error: Incorrect value

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: String!) { example(input: $var) }")
                    .SetVariableValue("var", "abc123")
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_and_B_is_set_to_string_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { a: \"abc\", b: \"123\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_to_string_Error()
    {
        Snapshot.FullName();

        // Error: Incorrect value for member field {b}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { b: \"123\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_to_string_Error()
    {
        Snapshot.FullName();

        // Error: Incorrect value for member field {b}
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { a: \"123\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_not_set_Error()
    {
        Snapshot.FullName();

        // Error: Value for member field {b} must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: Int!) { example(input: { b: $var }) }")
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Var_is_object_with_field_a_set_to_abc_Valid()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: ExampleInput!) { example(input: $var) }")
                    .SetVariableValue("var", new ObjectValueNode(new ObjectFieldNode("a", "abc")))
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task A_is_set_and_B_is_null_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { a: \"abc\", b: null }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_variable_and_var_is_null_Valid()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("query($var: Int) { example(input: { b: $var }) }")
                    .SetVariableValue("var", null)
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task B_is_set_and_C_is_invalid_prop_Error()
    {
        Snapshot.FullName();

        // Error: Exactly one key must be specified
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .ExecuteRequestAsync("{ example(input: { b: 123, c: \"xyz\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public void Oneof_Input_Objects_that_is_Valid()
        => ExpectValid(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String
                b: Int
            }");

    [Fact]
    public void Oneof_Input_Objects_must_have_nullable_fields()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String!
                b: Int
            }");

    [Fact]
    public void Oneof_Input_Objects_must_have_nullable_fields_with_two_fields_non_null()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String!
                b: Int!
            }");

    [Fact]
    public void Oneof_Input_Objects_must_have_nullable_fields_with_one_field_has_default()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String = ""a""
                b: Int
            }");

    [Fact]
    public void Oneof_Input_Objects_must_have_nullable_fields_with_two_fields_that_have_default()
        => ExpectError(
            @"type Query {
                foo(f: FooInput): String
            }

            input FooInput @oneOf {
                a: String = ""a""
                b: Int = 1
            }");

    [Fact]
    public void Oneof_generic_code_first_schema()
        => SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .ModifyOptions(o => o.EnableOneOf = true)
            .Create()
            .Print()
            .MatchSnapshot();


    [Fact]
    public async Task Oneof_introspection()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o =>
            {
                o.EnableOneOf = true;
                o.StrictValidation = true;
            })
            .ExecuteRequestAsync(
                @"{ 
                    oneof_input: __type(name: ""ExampleInput"") { 
                        # should be true
                        oneOf 
                    }

                    input: __type(name: ""StandardInput"") { 
                        # should be false
                        oneOf 
                    }

                    object: __type(name: ""Query"") { 
                        # should be null
                        oneOf 
                    }
                }")
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
}
