using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IdFilterTypeInterceptorTests
{
    [Fact]
    public async Task Filtering_Should_UseIdType_When_Specified()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<Foo>(x =>
                x.Field(y => y.Bar).Type<IdOperationFilterInputType>()))
            .AddFiltering()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Filtering_Should_InfereType_When_Annotated()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooId>())
            .AddFiltering()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Filtering_Should_InferType_When_AnnotatedGeneric()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooIdGeneric>())
            .AddFiltering()
            .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Filtering_Should_InferType_When_AnnotatedWith_Derived_IDAttribute()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooIdDerived>())
            .AddFiltering()
            .BuildSchemaAsync();

        var filterType = Assert.IsAssignableFrom<InputObjectType>(schema.Types["FooIdDerivedFilterInput"]);
        var fieldType = filterType.Fields["bar"].Type.NamedType();

        Assert.Equal("IdOperationFilterInput", fieldType.Name);
    }

    [Fact]
    public async Task Filtering_Should_InferType_When_AnnotatedWith_Derived_Generic_IDAttribute()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooIdGenericDerived>())
            .AddFiltering()
            .BuildSchemaAsync();

        var filterType = Assert.IsAssignableFrom<InputObjectType>(schema.Types["FooIdGenericDerivedFilterInput"]);
        var fieldType = filterType.Fields["bar"].Type.NamedType();

        Assert.Equal("IdOperationFilterInput", fieldType.Name);
    }

    public class Foo
    {
        public string? Bar { get; }
    }

    public class FooId
    {
        [ID]
        public string? Bar { get; }
    }

    public class FooIdGeneric
    {
        [ID<Foo>]
        public string? Bar { get; }
    }

    public class FooIdDerived
    {
        [InheritedId]
        public string? Bar { get; }
    }

    public class FooIdGenericDerived
    {
        [InheritedId<Foo>]
        public string? Bar { get; }
    }

    public sealed class InheritedId(string? typeName = null) : IDAttribute(typeName);

    public sealed class InheritedId<T> : IDAttribute<T>;
}
