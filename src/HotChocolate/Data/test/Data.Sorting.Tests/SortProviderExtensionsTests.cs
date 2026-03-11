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
            convention.DefinitionAccessor!.OperationHandlerConfigurations,
            x => Assert.Equal(extensionFieldHandler, x.Instance),
            x => Assert.Equal(firstFieldHandler, x.Instance));
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
            convention.DefinitionAccessor!.FieldHandlerConfigurations,
            x => Assert.Equal(extensionFieldHandler, x.Instance),
            x => Assert.Equal(firstFieldHandler, x.Instance));
    }

    private sealed class MockFieldHandler : QueryableDefaultSortFieldHandler;

    private sealed class MockProviderExtensions(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : SortProviderExtensions<QueryableSortContext>(configure);

    private sealed class MockProvider(
        Action<ISortProviderDescriptor<QueryableSortContext>> configure)
        : SortProvider<QueryableSortContext>(configure)
    {
        public SortProviderConfiguration? DefinitionAccessor => Configuration;

        public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
            => throw new NotImplementedException();
    }
}
