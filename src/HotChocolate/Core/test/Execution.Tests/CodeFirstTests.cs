using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class CodeFirstTests
    {
        [Fact]
        public async Task ExecuteOneFieldQueryWithProperty()
        {
            // arrange
            var schema = Schema.Create(
                c => c.RegisterType<QueryTypeWithProperty>());

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
            var schema = Schema.Create(
                c => c.RegisterType<QueryTypeWithMethod>());

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
            var schema = Schema.Create(
                c => c.RegisterType<QueryTypeWithMethod>());

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();
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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

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
            Schema schema = CreateSchema();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ dog { names } }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterType<QueryType>();
                c.RegisterType<FooType>();
                c.RegisterType<BarType>();
                c.RegisterType<FooBarUnionType>();
                c.RegisterType<DrinkType>();
                c.RegisterType<TeaType>();
                c.RegisterType<DogType>();
            });
        }

        public class Query
        {
            public string GetTest()
            {
                return "Hello World!";
            }

            public IQuery<string> GetQuery()
            {
                return MockQuery<string>.From("foo", "bar");
            }

            public string TestProp => "Hello World!";
        }

        public class QueryTypeWithProperty
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field(t => t.TestProp).Name("test");
            }
        }

        public class QueryTypeWithMethod
            : ObjectType<Query>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Query> descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field(t => t.GetTest()).Name("test");
                descriptor.Field(t => t.GetQuery()).Name("query");
            }
        }

        public class QueryType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("foo")
                    .Type<NonNullType<FooType>>()
                    .Resolver(() => "foo");
                descriptor.Field("bar")
                    .Type<NonNullType<BarType>>()
                    .Resolver(c => "bar");
                descriptor.Field("fooOrBar")
                    .Type<NonNullType<ListType<NonNullType<FooBarUnionType>>>>()
                    .Resolver(() => new object[] { "foo", "bar" });
                descriptor.Field("tea")
                    .Type<TeaType>()
                    .Resolver(() => "tea");
                descriptor.Field("drink")
                    .Type<DrinkType>()
                    .Resolver(() => "tea");
                descriptor.Field("dog")
                    .Type<DogType>()
                    .Resolver(() => new Dog());
            }
        }

        public class FooType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Field("bar")
                    .Type<NonNullType<FooType>>()
                    .Resolver(() => "bar");
                descriptor.Field("nameFoo").Resolver(() => "foo");
                descriptor.IsOfType((c, obj) => obj.Equals("foo"));
            }
        }

        public class BarType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Bar");
                descriptor.Field("foo")
                    .Type<NonNullType<FooType>>()
                    .Resolver(() => "foo");
                descriptor.Field("nameBar").Resolver(() => "bar");
                descriptor.IsOfType((c, obj) => obj.Equals("bar"));
            }
        }

        public class TeaType
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Tea");
                descriptor.Interface<DrinkType>();
                descriptor.Field("kind")
                    .Type<NonNullType<DrinkKindType>>()
                    .Resolver(() => DrinkKind.BlackTea);
                descriptor.IsOfType((c, obj) => obj.Equals("tea"));
            }
        }

        public class DrinkType
            : InterfaceType
        {
            protected override void Configure(
                IInterfaceTypeDescriptor descriptor)
            {
                descriptor.Name("Drink");
                descriptor.Field("kind")
                    .Type<NonNullType<DrinkKindType>>();
            }
        }

        public class DrinkKindType
            : EnumType<DrinkKind>
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

        public class FooBarUnionType
            : UnionType
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

        public class Dog
            : Pet
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

        public class DogType
            : ObjectType<Dog>
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

        public class MockQuery<T> : IQuery<T>
        {
            private readonly IReadOnlyList<T> _list;

            private MockQuery(IEnumerable<T> list)
            {
                _list = list.ToArray();
            }

            async ValueTask<object> IQuery.ExecuteAsync(CancellationToken cancellationToken)
            {
                return await ExecuteAsync(cancellationToken);
            }

            public ValueTask<IReadOnlyList<T>> ExecuteAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<IReadOnlyList<T>>(_list);
            }

            public string Print()
            {
                return _list.ToString();
            }

            public static MockQuery<T> From(params T[] items) => new MockQuery<T>(items);
        }
    }
}
