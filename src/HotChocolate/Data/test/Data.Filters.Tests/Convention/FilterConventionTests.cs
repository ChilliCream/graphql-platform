using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Filters;

public class FilterConventionTests
{
    [Fact]
    public void FilterConvention_Should_Work_When_ConfigurationIsComplete()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
        var type = new FooFilterInput();

        //act
        CreateSchemaWith(type, convention);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_OperationHandlerIsNotRegistered()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var type = new FooFilterInput();

        // act
        var error = Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        // assert
        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_FieldHandlerIsNotRegistered()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var type = new FooFilterInput();

        //act
        var error = Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        // assert
        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_OperationsInUknown()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var type = new FooFilterInput();

        //act
        var error = Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        // assert
        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_OperationsIsNotNamed()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Description("eq");
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var type = new FooFilterInput();

        //act
        Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_NoProviderWasRegistered()
    {
        // arrange
        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
            });

        var type = new FooFilterInput();

        //act
        var error = Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Fail_When_NoMatchingBindingWasFound()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.Provider(provider);
            });

        var type = new FooFilterInput();

        //act
        var error = Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

        // assert
        Assert.Single(error.Errors);
        error.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void FilterConvention_Should_Work_With_Extensions()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
            });

        var extension1 = new FilterConventionExtension(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var extension2 = new FilterConventionExtension(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
        var type = new FooFilterInput();

        //act
        CreateSchemaWith(type, convention, extension1, extension2);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void FilterConvention_Should_Work_With_ExtensionsType()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
        var type = new FooFilterInput();

        //act
        CreateSchemaWithTypes(
            type,
            convention,
            typeof(MockFilterExtensionConvention));
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void FilterConvention_Should_Work_With_ProviderExtensionsType()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor =>
            {
                descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
            });

        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.Provider(provider);
            });

        var extension1 = new FilterConventionExtension(
            descriptor =>
            {
                descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
                descriptor.AddProviderExtension<MockFilterProviderExtensionConvention>();
            });

        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
        var type = new FooFilterInput();

        //act
        CreateSchemaWith(type, convention, extension1);
        var executor = new ExecutorBuilder(type);

        var func = executor.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public async Task FilterConvention_Should_UseBoundFilterType()
    {
        // arrange
        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.AddDefaults();
                descriptor.BindRuntimeType<string, TestOperationFilterInputType>();
                descriptor.BindRuntimeType<Foo, CustomFooFilterInput>();
            });

        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                x => x.Name("Query").Field("foos").UseFiltering().Resolve(new List<Foo>()));

        //act
        var schema = await builder.BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void FilterProvider_Throws_Exception_When_NotInitializedByConvention()
    {
        // arrange
        var provider = new QueryableFilterProvider(
            descriptor => descriptor.AddFieldHandler<QueryableStringEqualsHandler>());
        var context = ConventionContext.Create(
            null,
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        // act
        provider.Initialize(context);

        // assert
        var exception =
            Assert.Throws<SchemaException>(() => provider.Complete(context));
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task FilterConvention_Should_NotAddAnd()
    {
        // arrange
        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.AddDefaults();
                descriptor.AllowAnd(false);
            });

        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                x => x.Name("Query").Field("foos").UseFiltering().Resolve(new List<Foo>()));

        //act
        var schema = await builder.BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task FilterConvention_Should_NotAddOr()
    {
        // arrange
        var convention = new FilterConvention(
            descriptor =>
            {
                descriptor.AddDefaults();
                descriptor.AllowOr(false);
            });

        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                x => x.Name("Query").Field("foos").UseFiltering().Resolve(new List<Foo>()));

        //act
        var schema = await builder.BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }

    protected ISchema CreateSchemaWithTypes(
        IFilterInputType type,
        FilterConvention convention,
        params Type[] extensions)
    {
        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
            .AddType(type);

        foreach (var extension in extensions)
        {
            builder.AddConvention<IFilterConvention>(extension);
        }

        return builder.Create();
    }

    protected ISchema CreateSchemaWith(
        IFilterInputType type,
        FilterConvention convention,
        params FilterConventionExtension[] extensions)
    {
        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .AddFiltering()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
            .AddType(type);

        foreach (var extension in extensions)
        {
            builder.AddConvention<IFilterConvention>(extension);
        }

        return builder.Create();
    }

    public class MockFilterProviderExtensionConvention : QueryableFilterProviderExtension
    {
        protected override void Configure(
            IFilterProviderDescriptor<QueryableFilterContext> descriptor)
        {
            descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
        }
    }

    public class MockFilterExtensionConvention : FilterConventionExtension
    {
        protected override void Configure(IFilterConventionDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
        }
    }

    public class TestOperationFilterInputType : StringOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }

    public class FailingCombinator
        : FilterOperationCombinator<FilterVisitorContext<string>, string>
    {
        public override bool TryCombineOperations(
            FilterVisitorContext<string> context,
            Queue<string> operations,
            FilterCombinator combinator,
            out string combined)
        {
            throw new NotImplementedException();
        }
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }

    public class CustomFooFilterInput : FilterInputType<Foo>
    {
    }
}
