using System;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Execution.Processing;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class ProjectionConventionExtensionsTests
{
    [Fact]
    public void Merge_Should_Merge_Provider()
    {
        // arrange
        var convention =
            new MockProjectionConvention(x => x.Provider<QueryableProjectionProvider>());
        var extension = new ProjectionConventionExtension(x => x.Provider<MockProvider>());
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.Equal(typeof(MockProvider), convention.DefinitionAccessor?.Provider);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderInstance()
    {
        // arrange
        var providerInstance = new MockProvider();
        var convention = new MockProjectionConvention(
            x => x.Provider(new QueryableProjectionProvider()));
        var extension = new ProjectionConventionExtension(
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
        Assert.Equal(providerInstance, convention.DefinitionAccessor?.ProviderInstance);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderExtensionsTypes()
    {
        // arrange
        var convention =
            new MockProjectionConvention(x => x.AddProviderExtension<MockProviderExtensions>());
        var extension =
            new ProjectionConventionExtension(
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
        Assert.NotNull(convention.DefinitionAccessor);
        Assert.Equal(2, convention.DefinitionAccessor!.ProviderExtensionsTypes.Count);
    }

    [Fact]
    public void Merge_Should_Merge_ProviderExtensions()
    {
        // arrange
        var provider1 = new MockProviderExtensions();
        var convention = new MockProjectionConvention(x => x.AddProviderExtension(provider1));
        var provider2 = new MockProviderExtensions();
        var extension =
            new ProjectionConventionExtension(x => x.AddProviderExtension(provider2));
        var context = new ConventionContext(
            "Scope",
            new ServiceCollection().BuildServiceProvider(),
            DescriptorContext.Create());

        convention.Initialize(context);
        extension.Initialize(context);

        // act
        extension.Merge(context, convention);

        // assert
        Assert.NotNull(convention.DefinitionAccessor);
        Assert.Collection(
            convention.DefinitionAccessor!.ProviderExtensions,
            x => Assert.Equal(provider1, x),
            x => Assert.Equal(provider2, x));
    }

    private sealed class MockProviderExtensions : ProjectionProviderExtension;

    private sealed class MockProvider : IProjectionProvider
    {
        public string? Scope => default;

        public IQueryBuilder CreateBuilder<TEntityType>()
            => throw new NotImplementedException();

        public Selection RewriteSelection(
            SelectionSetOptimizerContext context,
            Selection selection)
            => throw new NotImplementedException();
    }

    private sealed class MockProjectionConvention(
        Action<IProjectionConventionDescriptor> configure)
        : ProjectionConvention(configure)
    {
        public ProjectionConventionDefinition? DefinitionAccessor => Definition;
    }
}
