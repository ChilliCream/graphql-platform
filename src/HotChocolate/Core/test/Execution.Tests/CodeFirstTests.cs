using System.Collections;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Moq;

#nullable enable

namespace HotChocolate.Execution;

public class CodeFirstTests
{
    [Fact]
    public async Task ExecuteOneFieldQueryWithProperty()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryTypeWithProperty>()
            .Create();

        // act
        var result = await schema.MakeExecutable().ExecuteAsync("{ test }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AllowFiveToken_Success()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddSingleton(new ParserOptions(maxAllowedTokens: 5))
                .AddGraphQL()
                .AddQueryType<QueryTypeWithProperty>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ a: test }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
    }

    [Fact]
    public async Task AllowFiveToken_Fail()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddSingleton(new ParserOptions(maxAllowedTokens: 5))
                .AddGraphQL()
                .AddQueryType<QueryTypeWithProperty>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ a: test b: test }");

        // assert
        Assert.Collection(
            Assert.IsType<OperationResult>(result).Errors!,
            e => Assert.Equal("Document contains more than 5 tokens. Parsing aborted.", e.Message));
    }

    [Fact]
    public async Task AllowSixNode_Success()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddSingleton(new ParserOptions(maxAllowedNodes: 6))
                .AddGraphQL()
                .AddQueryType<QueryTypeWithProperty>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ a: test }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
    }

    [Fact]
    public async Task AllowSixNodes_Fail()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddSingleton(new ParserOptions(maxAllowedNodes: 6))
                .AddGraphQL()
                .AddQueryType<QueryTypeWithProperty>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ a: test b: test }");

        // assert
        Assert.Collection(
            Assert.IsType<OperationResult>(result).Errors!,
            e => Assert.Equal("Document contains more than 6 nodes. Parsing aborted.", e.Message));
    }

    [Fact]
    public async Task ExecuteOneFieldQueryWithMethod()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryTypeWithMethod>()
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync("{ test }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteOneFieldQueryWithQuery()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryTypeWithMethod>()
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync("{ query }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteWithUnionType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable()
                .ExecuteAsync(
                    @"
                        {
                            fooOrBar {
                                ... on Bar { nameBar }
                                ... on Foo { nameFoo }
                            }
                        }
                        ");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public void UnionTypeResolveType()
    {
        // arrange
        var schema = CreateSchema();

        var context = new Mock<IResolverContext>(
            MockBehavior.Strict);

        // act
        var fooBar = schema.GetType<UnionType>("FooBar");
        var teaType = fooBar.ResolveConcreteType(context.Object, "tea");
        var barType = fooBar.ResolveConcreteType(context.Object, "bar");

        // assert
        Assert.Null(teaType);
        Assert.NotNull(barType);
    }

    [Fact]
    public void UnionType_Contains_TypeName()
    {
        // arrange
        var schema = CreateSchema();

        var fooBar = schema.GetType<UnionType>("FooBar");

        // act
        var shouldBeFalse = fooBar.ContainsType("Tea");
        var shouldBeTrue = fooBar.ContainsType("Bar");

        // assert
        Assert.True(shouldBeTrue);
        Assert.False(shouldBeFalse);
    }

    [Fact]
    public void UnionType_Contains_ObjectType()
    {
        // arrange
        var schema = CreateSchema();

        var fooBar = schema.GetType<UnionType>("FooBar");
        var bar = schema.GetType<ObjectType>("Bar");
        var tea = schema.GetType<ObjectType>("Tea");

        // act
        var shouldBeTrue = fooBar.ContainsType(bar);
        var shouldBeFalse = fooBar.ContainsType(tea);

        // assert
        Assert.True(shouldBeTrue);
        Assert.False(shouldBeFalse);
    }

    [Fact]
    public void UnionType_Contains_IObjectType()
    {
        // arrange
        var schema = CreateSchema();

        IUnionType fooBar = schema.GetType<UnionType>("FooBar");
        IObjectType tea = schema.GetType<ObjectType>("Tea");
        IObjectType bar = schema.GetType<ObjectType>("Bar");

        // act
        var shouldBeFalse = fooBar.ContainsType(tea);
        var shouldBeTrue = fooBar.ContainsType(bar);

        // assert
        Assert.True(shouldBeTrue);
        Assert.False(shouldBeFalse);
    }

    [Fact]
    public async Task ExecuteWithInterface()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ drink { ... on Tea { kind } } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public void InterfaceTypeResolveType()
    {
        // arrange
        var schema = CreateSchema();
        var context = new Mock<IResolverContext>(
            MockBehavior.Strict);

        // act
        var drink = schema.GetType<InterfaceType>("Drink");
        var teaType = drink.ResolveConcreteType(context.Object, "tea");
        var barType = drink.ResolveConcreteType(context.Object, "bar");

        // assert
        Assert.NotNull(teaType);
        Assert.Null(barType);
    }

    [Fact]
    public async Task ExecuteImplicitField()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ dog { name } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteImplicitFieldWithNameAttribute()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ dog { desc } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteImplicitAsyncField()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ dog { name2 } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteExplicitAsyncField()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ dog { names } }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        await result.MatchSnapshotAsync();
    }

    [Fact]
    public async Task CannotCreateRootValue()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryPrivateConstructor>()
            .ExecuteRequestAsync("{ hello }")
            .MatchSnapshotAsync();
    }

    // https://github.com/ChilliCream/graphql-platform/issues/2617
    [Fact]
    public async Task EnsureThatFieldsWithDifferentCasingAreNotMerged()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryFieldCasing>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    // https://github.com/ChilliCream/graphql-platform/issues/2305
    [Fact]
    public async Task EnsureThatArgumentDefaultIsUsedWhenVariableValueIsOmitted()
    {
        var request =
            OperationRequestBuilder.Create()
                .SetDocument("query($v: String) { foo(value: $v) }")
                .Build();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithDefaultValue>()
            .ExecuteRequestAsync(request)
            .MatchSnapshotAsync();
    }

    private static ISchema CreateSchema()
        => SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType<BarType>()
            .AddType<FooBarUnionType>()
            .AddType<DrinkType>()
            .AddType<TeaType>()
            .AddType<DogType>()
            .Create();

    public class Query
    {
        public string GetTest()
        {
            return "Hello World!";
        }

        public IExecutable<string> GetQuery()
        {
            return new MockExecutable<string>(new[] { "foo", "bar", }.AsQueryable());
        }

        public string TestProp => "Hello World!";
    }

    public class QueryTypeWithProperty : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field(t => t.TestProp).Name("test");
        }
    }

    public class QueryTypeWithMethod : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field(t => t.GetTest()).Name("test");
            descriptor.Field(t => t.GetQuery()).Name("query");
        }
    }

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("foo")
                .Type<NonNullType<FooType>>()
                .Resolve(() => "foo");
            descriptor.Field("bar")
                .Type<NonNullType<BarType>>()
                .Resolve(c => "bar");
            descriptor.Field("fooOrBar")
                .Type<NonNullType<ListType<NonNullType<FooBarUnionType>>>>()
                .Resolve(() => new object[] { "foo", "bar", });
            descriptor.Field("tea")
                .Type<TeaType>()
                .Resolve(() => "tea");
            descriptor.Field("drink")
                .Type<DrinkType>()
                .Resolve(() => "tea");
            descriptor.Field("dog")
                .Type<DogType>()
                .Resolve(() => new Dog());
        }
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("bar")
                .Type<NonNullType<FooType>>()
                .Resolve(() => "bar");
            descriptor.Field("nameFoo").Resolve(() => "foo");
            descriptor.IsOfType((c, obj) => obj.Equals("foo"));
        }
    }

    public class BarType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Bar");
            descriptor.Field("foo")
                .Type<NonNullType<FooType>>()
                .Resolve(() => "foo");
            descriptor.Field("nameBar").Resolve(() => "bar");
            descriptor.IsOfType((c, obj) => obj.Equals("bar"));
        }
    }

    public class TeaType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Tea");
            descriptor.Implements<DrinkType>();
            descriptor.Field("kind")
                .Type<NonNullType<DrinkKindType>>()
                .Resolve(() => DrinkKind.BlackTea);
            descriptor.IsOfType((c, obj) => obj.Equals("tea"));
        }
    }

    public class DrinkType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Drink");
            descriptor.Field("kind")
                .Type<NonNullType<DrinkKindType>>();
        }
    }

    public class DrinkKindType : EnumType<DrinkKind>
    {
        protected override void Configure(
            IEnumTypeDescriptor<DrinkKind> descriptor)
        {
            descriptor.Name("DrinkKind");
        }
    }

    public enum DrinkKind
    {
        BlackTea,
        Water,
    }

    public class FooBarUnionType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("FooBar");
            descriptor.Type<BarType>();
            descriptor.Type<FooType>();
        }
    }

    public class Pet
    {
        public bool WithTail { get; set; }
    }

    public class Dog : Pet
    {
        public string Name { get; } = "a";

        [GraphQLName("desc")]
        public string Descriptor { get; } = "desc";

        public Task<string> GetName2()
        {
            return Task.FromResult("b");
        }

        public Task<IEnumerable<string>> GetNames()
        {
            return Task.FromResult<IEnumerable<string>>(new[] { "a", "b", });
        }
    }

    public class DogType : ObjectType<Dog>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Dog> descriptor)
        {
            descriptor.Field(t => t.WithTail)
                .Type<NonNullType<BooleanType>>();
            descriptor.Field(t => t.GetNames())
                .Type<ListType<StringType>>();
        }
    }

    public class MockExecutable<T>(IQueryable<T> source) : IExecutable<T>
    {
        public object Source => source;

        ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
            => new(source.ToList());

        public ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
            => new(source.ToList());

        ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.SingleOrDefault());

        public ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.SingleOrDefault());

        ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.FirstOrDefault());

        public ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
            => new(source.FirstOrDefault());

        public string Print()
            => source.ToString()!;
    }

    public class QueryPrivateConstructor
    {
        private QueryPrivateConstructor()
        {
        }

        public string Hello() => "Hello";
    }

    public class QueryFieldCasing
    {
        public string YourFieldName { get; set; } = default!;

        [GraphQLDeprecated("This is deprecated")]
        public string YourFieldname { get; set; } = default!;
    }

    public class QueryWithDefaultValue
    {
        public string Foo(string value = "abc") => value;
    }
}
