using System;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data
{
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
            var firstFieldHandler = new QueryableDefaultFieldHandler();
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

        private class MockFieldHandler : QueryableDefaultFieldHandler
        {

        }

        private class MockProviderExtensions
            : SortProviderExtensions<QueryableSortContext>
        {
            public MockProviderExtensions(
                Action<ISortProviderDescriptor<QueryableSortContext>> configure)
                : base(configure)
            {
            }
        }

        private class MockProvider : SortProvider<QueryableSortContext>
        {
            public SortProviderDefinition? DefinitionAccessor => base.Definition;

            public MockProvider(Action<ISortProviderDescriptor<QueryableSortContext>> configure)
                : base(configure)
            {
            }

            public override FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
            {
                throw new NotImplementedException();
            }
        }
    }
}
