using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class FilterConventionExtensionsTests
{
    [Fact]
    public void Merge_Should_Merge_ArgumentName()
    {
        // arrange
        var convention = new MockFilterConvention(x => x.ArgumentName("Foo"));
        var extension = new FilterConventionExtension(x => x.ArgumentName("Bar"));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.Equal("Bar", convention.ConfigurationAccessor?.ArgumentName);
    }

    [Fact]
    public void Merge_Should_NotMerge_ArgumentName_When_Default()
    {
        // arrange
        var convention = new MockFilterConvention(x => x.ArgumentName("Foo"));
        var extension = new FilterConventionExtension(
            x => x.ArgumentName(FilterConventionConfiguration.DefaultArgumentName));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.Equal("Foo", convention.ConfigurationAccessor?.ArgumentName);
    }

    [Fact]
    public void Merge_Should_Merge_Provider()
    {
        // arrange
        var convention = new MockFilterConvention(x => x.Provider<QueryableFilterProvider>());
        var extension = new FilterConventionExtension(x => x.Provider<MockProvider>());
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.Equal(typeof(MockProvider), convention.ConfigurationAccessor?.Provider);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderInstance()
    {
        // arrange
        var providerInstance = new MockProvider();
        var convention = new MockFilterConvention(
            x => x.Provider(new QueryableFilterProvider()));
        var extension = new FilterConventionExtension(
            x => x.Provider(providerInstance));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.Equal(providerInstance, convention.ConfigurationAccessor?.ProviderInstance);
    }

    [Fact]
    public void Merge_Should_Merge_Operations()
    {
        // arrange
        var convention = new MockFilterConvention(x => x.Operation(1));
        var extension = new FilterConventionExtension(x => x.Operation(2));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        Assert.Collection(
            convention.ConfigurationAccessor!.Operations,
            x => Assert.Equal(1, x.Id),
            x => Assert.Equal(2, x.Id));
    }

    [Fact]
    public void Merge_Should_Merge_Bindings()
    {
        // arrange
        var convention = new MockFilterConvention(
            x => x.BindRuntimeType<int, ComparableOperationFilterInputType<int>>());
        var extension = new FilterConventionExtension(
            x => x.BindRuntimeType<double, ComparableOperationFilterInputType<double>>());
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        Assert.Contains(typeof(int), convention.ConfigurationAccessor!.Bindings);
        Assert.Contains(typeof(double), convention.ConfigurationAccessor!.Bindings);
    }

    [Fact]
    public void Merge_Should_DeepMerge_Configurations()
    {
        // arrange
        var convention = new MockFilterConvention(
            x => x.Configure<ComparableOperationFilterInputType<int>>(d => d.Name("Foo")));
        var extension = new FilterConventionExtension(
            x => x.Configure<ComparableOperationFilterInputType<int>>(d => d.Name("Bar")));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        var configuration = Assert.Single(convention.ConfigurationAccessor!.Configurations.Values);
        Assert.Equal(2, configuration.Count);
    }

    [Fact]
    public void Merge_Should_Merge_Configurations()
    {
        // arrange
        var convention = new MockFilterConvention(
            x => x.Configure<ComparableOperationFilterInputType<int>>(d => d.Name("Foo")));
        var extension = new FilterConventionExtension(
            x => x.Configure<ComparableOperationFilterInputType<double>>(d => d.Name("Bar")));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        Assert.Equal(2, convention.ConfigurationAccessor!.Configurations.Count);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderExtensionsTypes()
    {
        // arrange
        var convention =
            new MockFilterConvention(x => x.AddProviderExtension<MockProviderExtensions>());
        var extension =
            new FilterConventionExtension(
                x => x.AddProviderExtension<MockProviderExtensions>());
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        Assert.Equal(2, convention.ConfigurationAccessor!.ProviderExtensionsTypes.Count);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderExtensions()
    {
        // arrange
        var provider1 = new MockProviderExtensions();
        var convention = new MockFilterConvention(x => x.AddProviderExtension(provider1));
        var provider2 = new MockProviderExtensions();
        var extension = new FilterConventionExtension(x => x.AddProviderExtension(provider2));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.ConfigurationAccessor);
        Assert.Collection(
            convention.ConfigurationAccessor!.ProviderExtensions,
            x => Assert.Equal(provider1, x),
            x => Assert.Equal(provider2, x));
    }

    private sealed class MockProviderExtensions : FilterProviderExtensions<QueryableFilterContext>;

    private sealed class MockProvider : IFilterProvider
    {
        public IReadOnlyCollection<IFilterFieldHandler> FieldHandlers => null!;

        public IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
            => throw new NotImplementedException();

        public void ConfigureField(string argumentName, IObjectFieldDescriptor descriptor)
            => throw new NotImplementedException();

        public IFilterMetadata? CreateMetaData(
            ITypeCompletionContext context,
            IFilterInputTypeConfiguration typeConfiguration,
            IFilterFieldConfiguration fieldConfiguration)
            => null;
    }

    private sealed class MockFilterConvention(
        Action<IFilterConventionDescriptor> configure)
        : FilterConvention(configure)
    {
        public FilterConventionConfiguration? ConfigurationAccessor => Configuration;
    }
}
