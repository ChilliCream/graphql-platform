using System;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class SortProviderExtensionsTests
{
    [Fact]
    public void Merge_Should_Merge_OperationHandlersAndPrependExtensionHandlers()
    {
        // arrange
        var firstFieldHandler = new QueryableAscendingSortOperationHandler();
        var extensionFieldHandler = new QueryableDescendingSortOperationHandler();
        var convention = new MockProvider(x => x.AddOperationHandler(firstFieldHandler));
        var extension = new MockProviderExtensions(
            x => x.AddOperationHandler(extensionFieldHandler));
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
            convention.DefinitionAccessor!.OperationHandlers,
            x => Assert.Equal(extensionFieldHandler, x.HandlerInstance),
            x => Assert.Equal(firstFieldHandler, x.HandlerInstance));
    }

    [Fact]
    public void Merge_Should_Merge_HandlersAndPrependExtensionHandlers()
    {
        // arrange
        var firstFieldHandler = new QueryableDefaultSortFieldHandler();
        var extensionFieldHandler = new MockFieldHandler();
        var convention = new MockProvider(x => x.AddFieldHandler(firstFieldHandler));
        var extension = new MockProviderExtensions(
            x => x.AddFieldHandler(extensionFieldHandler));
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
            convention.DefinitionAccessor!.Handlers,
            x => Assert.Equal(extensionFieldHandler, x.HandlerInstance),
            x => Assert.Equal(firstFieldHandler, x.HandlerInstance));
    }

    private sealed class MockFieldHandler : QueryableDefaultSortFieldHandler;

    private sealed class MockProviderExtensions(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : SortProviderExtensions<QueryableSortContext>(configure);

    private sealed class MockProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : SortProvider<QueryableSortContext>(configure)
    {
        public SortProviderDefinition? DefinitionAccessor => Definition;

        public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
            => throw new NotImplementedException();
    }
}
