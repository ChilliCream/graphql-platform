using System;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Data;

public class FilterProviderExtensionsTests
{
    [Fact]
    public void Merge_Should_Merge_HandlersAndPrependExtensionHandlers()
    {
        // arrange
        var typeConverterMock = new Mock<ITypeConverter>();
        var inputParser = new InputParser(typeConverterMock.Object);
        var firstFieldHandler = new QueryableStringContainsHandler(inputParser);
        var extensionFieldHandler = new QueryableStringContainsHandler(inputParser);
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

    private sealed class MockProviderExtensions
        : FilterProviderExtensions<QueryableFilterContext>
    {
        public MockProviderExtensions(
            Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
            : base(configure)
        {
        }
    }

    private sealed class MockProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : FilterProvider<QueryableFilterContext>(configure)
    {
        public FilterProviderDefinition? DefinitionAccessor => Definition;

        public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
            => throw new NotImplementedException();
    }
}
