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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        schema.MatchSnapshot();
    }

    [Fact]
    public void Interceptor_Should_PreserveCustomIdOperationType_When_ConfiguredWithGenericType()
        => AssertCustomIdOperationType<SubscriptionFilterType>();

    [Fact]
    public void Interceptor_Should_PreserveCustomIdOperationType_When_ConfiguredWithInstance()
        => AssertCustomIdOperationType<SubscriptionFilterTypeWithInstance>();

    [Fact]
    public void Interceptor_Should_PreserveCustomIdOperationType_When_GenericTypeIsNonNull()
        => AssertCustomIdOperationType<SubscriptionFilterTypeWithGenericNonNull>(isNonNull: true);

    [Fact]
    public void Interceptor_Should_PreserveCustomIdOperationType_When_InstanceTypeIsNonNull()
        => AssertCustomIdOperationType<SubscriptionFilterTypeWithInstanceNonNull>(isNonNull: true);

    [Fact]
    public async Task Filtering_Should_InferType_When_AnnotatedGeneric()
    {
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("test").Resolve("a"))
            .AddType(new FilterInputType<FooIdGeneric>())
            .AddFiltering()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

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

    private static void AssertCustomIdOperationType<TFilterType>(bool isNonNull = false)
    {
        var schema = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("subscriptions")
                    .Resolve(new List<SubscriptionNode>())
                    .UseFiltering<TFilterType>())
            .Create();
        var filterType = Assert.IsAssignableFrom<InputObjectType>(
            schema.Types["SubscriptionNodeFilterInput"]);
        var fieldType = filterType.Fields["id"].Type;

        Assert.Equal(nameof(SubscriptionIdOperationFilterInput), fieldType.NamedType().Name);
        Assert.Equal(isNonNull, fieldType is NonNullType);
    }

    public class SubscriptionNode
    {
        [ID]
        public int Id { get; set; }
    }

    public class SubscriptionFilterType : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type<SubscriptionIdOperationFilterInput>();
        }
    }

    public class SubscriptionFilterTypeWithInstance : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type(new SubscriptionIdOperationFilterInput());
        }
    }

    public class SubscriptionFilterTypeWithGenericNonNull : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type<NonNullType<SubscriptionIdOperationFilterInput>>();
        }
    }

    public class SubscriptionFilterTypeWithInstanceNonNull : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type(
                new NonNullType(new SubscriptionIdOperationFilterInput()));
        }
    }

    public class SubscriptionIdOperationFilterInput : IdOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<IdType>();
            descriptor.Operation(DefaultFilterOperations.In).Type<ListType<IdType>>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
