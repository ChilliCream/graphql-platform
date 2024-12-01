using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public class SortConventionTests
{
    [Fact]
    public void SortConvention_Should_Work_When_ConfigurationIsComplete()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: ASC}");
        var type = new FooSortType();

        //act
        CreateSchemaWith(type, convention);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new[] { new Foo { Bar = "a", }, new Foo { Bar = "b", }, new Foo { Bar = "c", }, };
        Assert.Collection(
            func(a),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));

        var b = new[] { new Foo { Bar = "c", }, new Foo { Bar = "b", }, new Foo { Bar = "a", }, };
        Assert.Collection(
            func(b),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));
    }

    [Fact]
    public void SortConvention_Should_Fail_When_OperationHandlerIsNotRegistered()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("asc");
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Fail_When_FieldHandlerIsNotRegistered()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("asc");
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Fail_When_OperationsInUknown()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Fail_When_OperationsIsNotNamed()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Description("asc");
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        error.Message.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Fail_When_NoProviderWasRegistered()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("asc");
                descriptor.BindRuntimeType<string, TestEnumType>();
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Fail_When_NoMatchingBindingWasFound()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("asc");
                descriptor.Provider(provider);
            });

        var type = new FooSortType();

        //act
        var error =
            Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void SortConvention_Should_Work_With_Extensions()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
            });

        var extension1 = new SortConventionExtension(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
                descriptor.Provider(provider);
            });

        var extension2 = new SortConventionExtension(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestEnumType>();
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: ASC}");
        var type = new FooSortType();

        //act
        CreateSchemaWith(type, convention, extension1, extension2);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new[] { new Foo { Bar = "a", }, new Foo { Bar = "b", }, new Foo { Bar = "c", }, };
        Assert.Collection(
            func(a),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));

        var b = new[] { new Foo { Bar = "c", }, new Foo { Bar = "b", }, new Foo { Bar = "a", }, };
        Assert.Collection(
            func(b),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));
    }

    [Fact]
    public void SortConvention_Should_Work_With_ExtensionsType()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: ASC}");
        var type = new FooSortType();

        //act
        CreateSchemaWithTypes(
            type,
            convention,
            typeof(MockSortExtensionConvention));
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new[] { new Foo { Bar = "a", }, new Foo { Bar = "b", }, new Foo { Bar = "c", }, };
        Assert.Collection(
            func(a),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));

        var b = new[] { new Foo { Bar = "c", }, new Foo { Bar = "b", }, new Foo { Bar = "a", }, };
        Assert.Collection(
            func(b),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));
    }

    [Fact]
    public void SortConvention_Should_Work_With_ProviderExtensionsType()
    {
        // arrange
        var provider = new QueryableSortProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
            });

        var convention = new SortConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestEnumType>();
                descriptor.Provider(provider);
            });

        var extension1 = new SortConventionExtension(
            descriptor =>
            {
                descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
                descriptor.AddProviderExtension<MockSortProviderExtensionConvention>();
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: ASC}");
        var type = new FooSortType();

        //act
        CreateSchemaWith(type, convention, extension1);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new[] { new Foo { Bar = "a", }, new Foo { Bar = "b", }, new Foo { Bar = "c", }, };
        Assert.Collection(
            func(a),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));

        var b = new[] { new Foo { Bar = "c", }, new Foo { Bar = "b", }, new Foo { Bar = "a", }, };
        Assert.Collection(
            func(b),
            x => Assert.Equal("a", x.Bar),
            x => Assert.Equal("b", x.Bar),
            x => Assert.Equal("c", x.Bar));
    }

    protected ISchema CreateSchemaWithTypes(
        ISortInputType type,
        SortConvention convention,
        params Type[] extensions)
    {
        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .AddSorting()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
            .AddType(type);

        foreach (var extension in extensions)
        {
            builder.AddConvention<ISortConvention>(extension);
        }

        return builder.Create();
    }

    protected ISchema CreateSchemaWith(
        ISortInputType type,
        SortConvention convention,
        params SortConventionExtension[] extensions)
    {
        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .AddSorting()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
            .AddType(type);

        foreach (var extension in extensions)
        {
            builder.AddConvention<ISortConvention>(extension);
        }

        return builder.Create();
    }

    public class MockSortProviderExtensionConvention : QueryableSortProviderExtension
    {
        protected override void Configure(
            ISortProviderDescriptor<QueryableSortContext> descriptor)
        {
            descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
        }
    }

    public class MockSortExtensionConvention : SortConventionExtension
    {
        protected override void Configure(ISortConventionDescriptor descriptor)
        {
            descriptor.Operation(DefaultSortOperations.Ascending).Name("ASC");
        }
    }

    public class TestEnumType : SortEnumType
    {
        protected override void Configure(ISortEnumTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultSortOperations.Ascending);
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class FooSortType
        : SortInputType<Foo>
    {
        protected override void Configure(
            ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
