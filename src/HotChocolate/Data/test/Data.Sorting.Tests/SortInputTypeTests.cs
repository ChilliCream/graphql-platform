using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests;

public class SortInputTypeTest : SortTestBase
{
    [Fact]
    public void SortInputType_DynamicName()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new SortInputType<Foo>(
                    d => d
                        .Name(dep => dep.Name + "Foo")
                        .DependsOn<StringType>()
                        .Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInputType_DynamicName_NonGeneric()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new SortInputType<Foo>(
                    d => d.Name(dep => dep.Name + "Foo")
                        .DependsOn(typeof(StringType))
                        .Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new SortInputType<Foo>(
                        d => d.Directive("foo")
                            .Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s
                .AddDirectiveType<FooDirectiveType>()
                .AddType(new SortInputType<Foo>(d => d.Directive("foo").Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new SortInputType<Foo>(
                        d => d.Directive(new DirectiveNode("foo")).Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new SortInputType<Foo>(
                        d => d
                            .Directive(new FooDirective())
                            .Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddDirectiveType<FooDirectiveType>()
                .AddType(
                    new SortInputType<Foo>(
                        d => d
                            .Directive<FooDirective>()
                            .Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddDescription()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new SortInputType<Foo>(
                    d => d.Description("Test").Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInput_AddName()
    {
        // arrange
        // act
        var schema = CreateSchema(
            s => s.AddType(
                new SortInputType<Foo>(
                    d => d.Name("Test").Field(x => x.Bar))));

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInputType_Should_ThrowException_WhenNoConventionIsRegistered()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseSorting("Foo"));

        // act
        // assert
        var exception = Assert.Throws<SchemaException>(() => builder.Create());
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void SortInputType_Should_ThrowException_WhenNoConventionIsRegisteredDefault()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Resolve(new List<Foo>())
                        .UseSorting());

        // act
        // assert
        var exception = Assert.Throws<SchemaException>(() => builder.Create());
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void SortInputType_Should_UseCustomSortType_When_Nested()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType<UserQueryType>();

        // act
        // assert
        builder.Create().Print().MatchSnapshot();
    }

    [Fact]
    public void SortInputType_Should_IgnoreFieldWithoutCallingConvention()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddSorting(
                x => x.AddDefaultOperations()
                    .BindRuntimeType<int, DefaultSortEnumType>()
                    //should fail when not ignore properly because string is no explicitly bound
                    .Provider(new QueryableSortProvider(y => y.AddDefaultFieldHandlers())))
            .AddQueryType(
                new ObjectType(
                    x => x.Name("Query")
                        .Field("foo")
                        .Resolve(new List<IgnoreTest>())
                        .UseSorting<IgnoreTestSortInputType>()));

        // act
        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void SortInputType_Should_InfereType_When_ItIsAInterface()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType<TestingType<ITest<Foo>>>()
            .AddObjectType<ITest<Foo>>();

        // act
        var schema = builder.Create();

        // assert
        schema.MatchSnapshot();
        schema.Print().MatchSnapshot();
    }

    public class IgnoreTest
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class ShouldNotBeVisible : SortInputType;

    public class IgnoreTestSortInputType : SortInputType<IgnoreTest>
    {
        protected override void Configure(ISortInputTypeDescriptor<IgnoreTest> descriptor)
            => descriptor.Ignore(x => x.Name);
    }

    public class FooDirectiveType : DirectiveType<FooDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor.Name("foo");
            descriptor.Location(Types.DirectiveLocation.InputObject)
                .Location(Types.DirectiveLocation.InputFieldDefinition);
        }
    }

    public class FooDirective;

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class Query
    {
        [GraphQLNonNullType]
        public IQueryable<Book> Books() => new List<Book>().AsQueryable();
    }

    public class Book
    {
        public int Id { get; set; } = default!;

        [GraphQLNonNullType]
        public string Title { get; set; } = default!;

        public int Pages { get; set; } = default!;

        public int Chapters { get; set; } = default!;

        [GraphQLNonNullType]
        public Author Author { get; set; } = default!;
    }

    public class Author
    {
        [GraphQLType(typeof(NonNullType<IdType>))]
        public int Id { get; set; }

        [GraphQLNonNullType]
        public string Name { get; set; } = default!;

        public User? Account { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public List<User> Friends { get; set; } = default!;
    }

    public interface ITest
    {
        public string? Prop { get; set; }
        public string? Prop2 { get; set; }
    }

    public interface ITest<T>
    {
        T Prop { get; set; }
    }

    public class InterfaceImpl1 : ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class InterfaceImpl2 : ITest
    {
        public string? Prop { get; set; }

        public string? Prop2 { get; set; }
    }

    public class UserSortInputType : SortInputType<User>
    {
        protected override void Configure(ISortInputTypeDescriptor<User> descriptor)
        {
            descriptor.Ignore(x => x.Id);
        }
    }

    public class UserQueryType : ObjectType<User>
    {
        protected override void Configure(IObjectTypeDescriptor<User> descriptor)
        {
            descriptor.Name(nameof(Query));
            descriptor
                .Field("foo")
                .Resolve(new List<User>())
                .UseSorting<UserSortInputType>();
        }
    }

    public class TestObject<T>
    {
        public T Root { get; set; } = default!;
    }

    public class TestingType<T> : ObjectType<TestObject<T>>
    {
        protected override void Configure(IObjectTypeDescriptor<TestObject<T>> descriptor)
        {
            descriptor.Name(nameof(Query));
            descriptor.Field(x => x.Root).UseFiltering();
        }
    }
}
