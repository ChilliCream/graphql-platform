using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

#nullable enable

namespace HotChocolate.Execution
{
    public class CodeFirstTests
    {
        [Fact]
        public async Task ExecuteOneFieldQueryWithProperty()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryTypeWithProperty>()
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync("{ test }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteOneFieldQueryWithMethod()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryTypeWithMethod>()
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync("{ test }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteOneFieldQueryWithQuery()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryTypeWithMethod>()
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync("{ query }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteWithUnionType()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
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
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public void UnionTypeResolveType()
        {
            // arrange
            ISchema schema = CreateSchema();

            var context = new Mock<IResolverContext>(
                MockBehavior.Strict);

            // act
            UnionType fooBar = schema.GetType<UnionType>("FooBar");
            ObjectType teaType = fooBar.ResolveConcreteType(context.Object, "tea");
            ObjectType barType = fooBar.ResolveConcreteType(context.Object, "bar");

            // assert
            Assert.Null(teaType);
            Assert.NotNull(barType);
        }

        [Fact]
        public void UnionType_Contains_TypeName()
        {
            // arrange
            ISchema schema = CreateSchema();

            var context = new Mock<IResolverContext>(
                MockBehavior.Strict);
            UnionType fooBar = schema.GetType<UnionType>("FooBar");

            // act
            bool shouldBeFalse = fooBar.ContainsType("Tea");
            bool shouldBeTrue = fooBar.ContainsType("Bar");

            // assert
            Assert.True(shouldBeTrue);
            Assert.False(shouldBeFalse);
        }

        [Fact]
        public void UnionType_Contains_ObjectType()
        {
            // arrange
            ISchema schema = CreateSchema();

            var context = new Mock<IResolverContext>(
                MockBehavior.Strict);
            UnionType fooBar = schema.GetType<UnionType>("FooBar");
            ObjectType bar = schema.GetType<ObjectType>("Bar");
            ObjectType tea = schema.GetType<ObjectType>("Tea");

            // act
            bool shouldBeTrue = fooBar.ContainsType(bar);
            bool shouldBeFalse = fooBar.ContainsType(tea);

            // assert
            Assert.True(shouldBeTrue);
            Assert.False(shouldBeFalse);
        }

        [Fact]
        public void UnionType_Contains_IObjectType()
        {
            // arrange
            ISchema schema = CreateSchema();

            var context = new Mock<IResolverContext>(
                MockBehavior.Strict);
            IUnionType fooBar = schema.GetType<UnionType>("FooBar");
            IObjectType tea = schema.GetType<ObjectType>("Tea");
            IObjectType bar = schema.GetType<ObjectType>("Bar");

            // act
            bool shouldBeFalse = fooBar.ContainsType(tea);
            bool shouldBeTrue = fooBar.ContainsType(bar);

            // assert
            Assert.True(shouldBeTrue);
            Assert.False(shouldBeFalse);
        }

        [Fact]
        public async Task ExecuteWithInterface()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ drink { ... on Tea { kind } } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public void InterfaceTypeResolveType()
        {
            // arrange
            ISchema schema = CreateSchema();
            var context = new Mock<IResolverContext>(
                MockBehavior.Strict);

            // act
            InterfaceType drink = schema.GetType<InterfaceType>("Drink");
            ObjectType teaType = drink.ResolveConcreteType(context.Object, "tea");
            ObjectType barType = drink.ResolveConcreteType(context.Object, "bar");

            // assert
            Assert.NotNull(teaType);
            Assert.Null(barType);
        }

        [Fact]
        public async Task ExecuteImplicitField()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ dog { name } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteImplicitFieldWithNameAttribute()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ dog { desc } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteImplicitAsyncField()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ dog { name2 } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteExplicitAsyncField()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ dog { names } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
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

        // https://github.com/ChilliCream/hotchocolate/issues/2617
        [Fact]
        public async Task EnsureThatFieldsWithDifferentCasingAreNotMerged()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryFieldCasing>()
                .BuildSchemaAsync()
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
                return new MockExecutable<string>(new[] { "foo", "bar" }.AsQueryable());
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
                    .Resolve(() => new object[] { "foo", "bar" });
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
            Water
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
                return Task.FromResult<IEnumerable<string>>(new[] { "a", "b" });
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

        public class MockExecutable<T> : IExecutable<T>
        {
            private readonly IQueryable<T> _source;

            public MockExecutable(IQueryable<T> source)
            {
                _source = source;
            }

            public object Source => _source;

            public ValueTask<IList> ToListAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<IList>(_source.ToList());
            }

            public ValueTask<object?> FirstOrDefaultAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<object?>(_source.FirstOrDefault());
            }

            public ValueTask<object?> SingleOrDefaultAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<object?>(_source.SingleOrDefault());
            }

            public string Print()
            {
                return _source.ToString();
            }
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
            public string YourFieldName { get; set; }

            [GraphQLDeprecated("This is deprecated")]
            public string YourFieldname { get; set; }
        }
    }
}
