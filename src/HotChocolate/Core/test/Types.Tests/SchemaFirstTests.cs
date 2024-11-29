using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate;

public class SchemaFirstTests
{
    [Fact]
    public async Task DescriptionsAreCorrectlyRead()
    {
        // arrange
        var source = FileResource.Open("schema_with_multiline_descriptions.graphql");
        var query = FileResource.Open("IntrospectionQuery.graphql");

        // act & act
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(source)
            .ModifyOptions(o => o.SortFieldsByName = true)
            .UseField(next => next)
            .ExecuteRequestAsync(query)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Interfaces_Impl_Interfaces_Are_Correctly_Exposed_Through_Introspection()
    {
        // arrange
        var source = @"
                type Query {
                    c: C
                }

                interface A {
                    a: String
                }

                interface B implements A {
                    a: String
                }

                type C implements A & B {
                    a: String
                }
            ";
        var query = FileResource.Open("IntrospectionQuery.graphql");

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .Use(next => next)
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var executor = schema.MakeExecutable();
        var result = await executor.ExecuteAsync(query);
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Against_Schema_With_Interface_Schema()
    {
        var source = @"
                type Query {
                    pet: Pet
                }

                interface Pet {
                    name: String
                }

                type Cat implements Pet {
                    name: String
                }

                type Dog implements Pet {
                    name: String
                }
            ";

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(source)
            .AddResolver<PetQuery>("Query")
            .BindRuntimeType<Cat>()
            .BindRuntimeType<Dog>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Execute_Against_Schema_With_Interface_Execute()
    {
        var source = @"
                type Query {
                    pet: Pet
                }

                interface Pet {
                    name: String
                }

                type Cat implements Pet {
                    name: String
                }

                type Dog implements Pet {
                    name: String
                }
            ";

        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(source)
            .AddResolver<PetQuery>("Query")
            .BindRuntimeType<Cat>()
            .BindRuntimeType<Dog>()
            .ExecuteRequestAsync("{ pet { name __typename } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SchemaDescription()
    {
        // arrange
        var sourceText = "\"\"\"\nMy Schema Description\n\"\"\"" +
            "schema" +
            "{ query: Foo }" +
            "type Foo { bar: String }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .Use(next => next)
            .Create();

        // assert
        var executor = schema.MakeExecutable();
        var result =
            await executor.ExecuteAsync("{ __schema { description } }");
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaBuilder_BindType()
    {
        // arrange
        var sourceText = "type Query { hello: String }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .AddRootResolver(new { Query = new Query(), })
            .Create();

        // assert
        var executor = schema.MakeExecutable();
        var result =
            await executor.ExecuteAsync("{ hello }");
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaBuilder_AddResolver()
    {
        // arrange
        var sourceText = "type Query { hello: String }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .AddResolver("Query", "hello", () => "World")
            .Create();

        // assert
        var executor = schema.MakeExecutable();
        var result =
            await executor.ExecuteAsync("{ hello }");
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void BuiltInScalarsAreRecognized()
    {
        // arrange
        var sourceText = @"
                type Query {
                    string_field: String
                    string_non_null_field: String!
                    int_field: Int
                    int_non_null_field: Int!
                    float_field: Float
                    float_non_null_field: Float!
                    bool_field: Boolean
                    bool_non_null_field: Boolean!
                }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .Use(_ => _)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void BuiltInScalarsAreRecognized2()
    {
        // arrange
        var sourceText = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    string_field: String
                    string_non_null_field: String!
                    int_field: Int
                    int_non_null_field: Int!
                    float_field: Float
                    float_non_null_field: Float!
                    bool_field: Boolean
                    bool_non_null_field: Boolean!
                }
            ";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .Use(_ => _)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ListTypesAreRecognized()
    {
        // arrange
        var sourceText = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    single_int: Int
                    list_int: [Int]
                    matrix_int: [[Int]]
                    nullable_single_int: Int!
                    nullable_list_int: [Int!]!
                    nullable_matrix_int: [[Int!]!]!
                }
            ";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .Use(_ => _)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaBuilder_AnyType()
    {
        // arrange
        var sourceText = "type Query { hello: Any }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(sourceText)
            .AddResolver<Query>()
            .Create();

        // assert
        var executor = schema.MakeExecutable();
        var result = await executor.ExecuteAsync("{ hello }");
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_Paging()
    {
        // arrange
        var sdl = "type Query { items: [String!] }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<QueryWithItems>("Query")
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_OffsetPaging()
    {
        // arrange
        var sdl = "type Query { items: [String!] }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<QueryWithOffsetItems>("Query")
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_Paging_With_Objects()
    {
        // arrange
        var sdl = "type Query { items: [Person!] } type Person { name: String }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<QueryWithPersons>("Query")
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_Paging_Execute()
    {
        // arrange
        var sdl = "type Query { items: [String!] }";

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<QueryWithItems>("Query")
                .ExecuteRequestAsync("{ items { nodes } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_Paging_With_Objects_Execute()
    {
        // arrange
        var sdl = "type Query { items: [Person!] } type Person { name: String }";

        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .BindRuntimeType<QueryWithPersons>("Query")
                .ExecuteRequestAsync("{ items { nodes { name } } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Cursor_Paging_With_Resolver()
    {
        // arrange
        var sdl = "type Query { items: [String!] }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddResolver<QueryWithItems>("Query")
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Reference_Schema_First_Types_From_Code_First_Models()
    {
        // arrange
        var sdl = "type Person { name: String! }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddQueryType<QueryCodeFirst>()
                .BindRuntimeType<Person>()
                .BuildSchemaAsync();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Apply_Schema_Building_Directive()
    {
        // arrange
        var sdl = "type Person { name: String! @desc(value: \"abc\") }";

        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(sdl)
                .AddQueryType<QueryCodeFirst>()
                .BindRuntimeType<Person>()
                .ConfigureSchema(sb => sb.TryAddSchemaDirective(new CustomDescriptionDirective()))
                .BuildSchemaAsync();

        // assert
        Assert.Equal(
            "abc",
            schema.GetType<ObjectType>("Person")?.Fields["name"].Description);
    }

    [Fact]
    public async Task Ensure_Input_Only_Enums_Are_Correctly_Bound()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                    type Query {
                        book(input: TestEnumInput): TestEnum
                    }

                    enum TestEnumInput { FOO_BAR_INPUT }
                    enum TestEnum { FOO_BAR }")
            .AddResolver<QueryEnumExample>("Query")
            .ExecuteRequestAsync("{ book(input: FOO_BAR_INPUT) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Default_Values_With_Inputs_Are_Applied()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                    type Query {
                        book(input: Foo): String
                    }

                    input Foo { bar: String = ""baz"" }")
            .AddResolver<QueryWithFooInput>("Query")
            .ExecuteRequestAsync("{ book(input: { }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Default_Values_With_Inputs_Can_Be_Overridden()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                    type Query {
                        book(input: Foo): String
                    }

                    input Foo { bar: String = ""baz"" }")
            .AddResolver<QueryWithFooInput>("Query")
            .ExecuteRequestAsync("{ book(input: { bar: \"baz123\" }) }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_Input_Only_Enums_Are_Correctly_Bound_When_Using_BindRuntimeType()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(@"
                    type Query {
                        book(input: TestEnumInput): TestEnum
                    }

                    enum TestEnumInput { FOO_BAR_INPUT }
                    enum TestEnum { FOO_BAR }")
            .BindRuntimeType<QueryEnumExample>("Query")
            .ExecuteRequestAsync("{ book(input: FOO_BAR_INPUT) }")
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public string Hello() => "World";
    }

    public class QueryWithItems
    {
        [UsePaging]
        public string[] GetItems() => ["a", "b",];
    }

    public class QueryWithOffsetItems
    {
        [UseOffsetPaging]
        public string[] GetItems() => ["a", "b",];
    }

    public class QueryWithPersons
    {
        [UsePaging]
        public Person[] GetItems() => [new Person { Name = "Foo", },];
    }

    public class QueryWithOffsetPersons
    {
        [UseOffsetPaging]
        public Person[] GetItems() => [new Person { Name = "Foo", },];
    }

    public class QueryCodeFirst
    {
        [GraphQLType("Person!")]
        public object GetPerson() => new Person { Name = "Hello", };
    }

    public class Person
    {
        public string Name { get; set; }
    }

    public class CustomDescriptionDirective : ISchemaDirective
    {
        public string Name => "desc";

        public void ApplyConfiguration(
            IDescriptorContext context,
            DirectiveNode directiveNode,
            IDefinition definition,
            Stack<IDefinition> path)
        {
            if (definition is ObjectFieldDefinition objectField)
            {
                objectField.Description = (string)directiveNode.Arguments.First().Value.Value;
            }
        }
    }

    public class PetQuery
    {
        public IPet GetPet() => new Cat("Mauzi");
    }

    public interface IPet
    {
        string Name { get; }
    }

    public class Cat : IPet
    {
        public Cat(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class Dog : IPet
    {
        public Dog(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}

public class QueryEnumExample
{
    public TestEnum GetBook(TestEnumInput input)
    {
        return TestEnum.FooBar;
    }
}

public enum TestEnum
{
    FooBar,
}

public enum TestEnumInput
{
    FooBarInput,
}

public class QueryWithFooInput
{
    public string GetBook(Foo input) => input.Bar;
}

public class Foo
{
    public Foo(string bar)
    {
        Bar = bar;
    }

    public string Bar { get; }
}
