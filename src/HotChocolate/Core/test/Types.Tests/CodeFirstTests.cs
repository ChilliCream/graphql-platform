#nullable enable

using System.Collections;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public class CodeFirstTests
{
    [Fact]
    public void InferSchemaWithNonNullRefTypes()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<Dog>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void InferSchemaWithNonNullRefTypesAndGenerics()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithGenerics>()
            .AddType<Dog>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Type_Is_Correctly_Upgraded()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .AddType<Dog>()
            .AddType<ObjectType<Dog>>()
            .AddType<DogType>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Change_DefaultBinding_For_DateTime()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithDateTimeType>()
            .BindRuntimeType<DateTime, DateTimeType>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Remove_ClrType_Bindings_That_Are_Not_Used()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDateTimeType>()
            .BindRuntimeType<DateTime, DateTimeType>()
            .BindRuntimeType<int, UrlType>()
            .ModifyOptions(o => o.RemoveUnreachableTypes = true)
            .Create();

        // assert
        var exists = schema.TryGetType<INamedType>("Url", out _);
        Assert.False(exists);
    }

    [Fact]
    public void Infer_Interface_Usage()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryInterfaces>()
            .AddType<Foo>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Infer_Interface_Usage_With_Interfaces_Implementing_Interfaces()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryInterfaces>()
            .AddType<Foo>()
            .AddType<IBar>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Default_Type_Resolution_Shall_Be_Exact()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("shouldBeCat").Type<InterfaceType<IPet>>().Resolve(new Cat());
                    d.Field("shouldBeDog").Type<InterfaceType<IPet>>().Resolve(new Dog());
                })
                .AddType<Dog>()
                .AddType<Cat>()
                .ExecuteRequestAsync("{ shouldBeCat { __typename } shouldBeDog { __typename } }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Default_Type_Resolution_Shall_Be_Exact_Schema()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("shouldBeCat").Type<InterfaceType<IPet>>().Resolve(new Cat());
                    d.Field("shouldBeDog").Type<InterfaceType<IPet>>().Resolve(new Dog());
                })
                .AddType<Dog>()
                .AddType<Cat>()
                .BuildSchemaAsync();

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Structural_Equality_Is_Ignored()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryStructEquals>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Comparison_Is_Ignored()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryComparableEntity>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Allow_PascalCasedArguments_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<PascalCaseQuery>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Allow_PascalCasedArguments()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<PascalCaseQuery>()
                .ExecuteRequestAsync(
                    """
                    {
                        testResolver(testArgument: "abc")
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "testResolver": "abc"
              }
            }
            """);
    }

    [Fact]
    public async Task Infer_Nullability_From_Nested_Classes()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryNestedClassNullableString>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task EnumerableArgs_Are_Inferred_As_List()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryWithEnumerableArg>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: QueryWithEnumerableArg
            }

            type QueryWithEnumerableArg {
              foo(foo: [String!]!): String!
            }
            """);
    }

    public class Query
    {
        public string SayHello(string name) =>
            throw new NotImplementedException();

        public Greetings GetGreetings(Greetings greetings) =>
            throw new NotImplementedException();

        public IPet GetPet() =>
            throw new NotImplementedException();

        public Task<IPet?> GetPetOrNull() =>
            throw new NotImplementedException();
    }

    public class QueryWithGenerics
    {
        public Task<IPet?> GetPet(int id, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        // The arguments are needed to make the compiler apply attributes as expected
        // for this use case. It's not entirely clear what combination of arguments
        // for this and other fields on the class makes it behave this way
        // We want the compiler to apply these attributes to the GetPets method
        // [NullableContext(2)]
        // [return: Nullable(1)]
        public Task<GenericWrapper<IPet>> GetPets(
            int? arg1,
            bool? arg2,
            bool? arg3,
            string? arg4,
            GenericWrapper<string>? arg5,
            Greetings? arg6,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    public class QueryWithEnumerableArg
    {
        public string GetFoo(IEnumerable<string> foo)
            => "foo";
    }

    public class GenericWrapper<T>
    {
        public T Value { get; set; } = default!;
    }

    public class Greetings
    {
        public string Name { get; set; } = "Foo";
    }

    public interface IPet
    {
        string? Name { get; }
    }

    public class DogType : ObjectType<Dog>
    {
        protected override void Configure(IObjectTypeDescriptor<Dog> descriptor)
        {
            descriptor.Field("isMale").Resolve(true);
        }
    }

    public class Dog : IPet
    {
        public string? Name =>
            throw new NotImplementedException();
    }

    public class Cat : Dog
    {
    }

    public class QueryWithDateTimeType : ObjectType<QueryWithDateTime>
    {
        protected override void Configure(IObjectTypeDescriptor<QueryWithDateTime> descriptor)
        {
            descriptor.Field(t => t.GetModel()).Type<ModelWithDateTimeType>();
        }
    }

    public class QueryWithDateTime
    {
        public ModelWithDateTime GetModel() => new ModelWithDateTime();
    }

    public class ModelWithDateTimeType : ObjectType<ModelWithDateTime>
    {
        protected override void Configure(IObjectTypeDescriptor<ModelWithDateTime> descriptor)
        {
            descriptor.Field(t => t.Foo).Type<DateType>();
        }
    }

    public class ModelWithDateTime
    {
        public DateTime Foo { get; set; }

        public DateTime Bar { get; set; }
    }

    public class QueryInterfaces
    {
        public IFoo GetFoo() => new Foo();
    }

    public class Foo : IFoo
    {
        public string GetBar() => "Bar";

        public string GetFoo() => "Foo";
    }

    public interface IFoo : IBar
    {
        string GetFoo();
    }

    public interface IBar
    {
        string GetBar();
    }

    public class QueryStructEquals
    {
        public EquatableExample Foo(EquatableExample example) => example;
    }

    public class EquatableExample : IStructuralEquatable
    {
        public string Some { get; set; } = default!;

        public bool Equals(object? other, IEqualityComparer comparer) => throw new NotImplementedException();

        public int GetHashCode(IEqualityComparer comparer) => throw new NotImplementedException();
    }

    public class QueryComparableEntity
    {
        public ComparableExample Foo(ComparableExample example) => example;
    }

    public class ComparableExample : IComparable, IComparable<EquatableExample>
    {
        public string Some { get; set; } = default!;

        public int CompareTo(object? obj)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(EquatableExample? other)
        {
            throw new NotImplementedException();
        }
    }

    public class PascalCaseQuery
    {
        public string TestResolver(string TestArgument) => "abc";
    }

    public sealed class QueryNestedClassNullableString
    {
        public class Outer
        {
            public string? shouldBeNullable { get; set; }
        }

        public class Example
        {
            public class Inner
            {
                public string? shouldAlsoBeNullable { get; set; }
            }

            public Inner? inner { get; set; } = new();
            public Outer? outer { get; set; } = new();
        }

        public Example NestedClassNullableString()
        {
            return new Example();
        }
    }
}
