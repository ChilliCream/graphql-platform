using System;
using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data
{
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
            Assert.Equal("Bar", convention.DefinitionAccessor?.ArgumentName);
        }

        [Fact]
        public void Merge_Should_NotMerge_ArgumentName_When_Default()
        {
            // arrange
            var convention = new MockFilterConvention(x => x.ArgumentName("Foo"));
            var extension = new FilterConventionExtension(
                x => x.ArgumentName(FilterConventionDefinition.DefaultArgumentName));
            var context = new ConventionContext(
                "Scope",
                new ServiceCollection().BuildServiceProvider(),
                DescriptorContext.Create());

            convention.Initialize(context);
            extension.Initialize(context);

            // act
            extension.Merge(context, convention);

            // assert
            Assert.Equal("Foo", convention.DefinitionAccessor?.ArgumentName);
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
            Assert.Equal(typeof(MockProvider), convention.DefinitionAccessor?.Provider);
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
            Assert.Equal(providerInstance, convention.DefinitionAccessor?.ProviderInstance);
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
            Assert.NotNull(convention.DefinitionAccessor);
            Assert.Collection(
                convention.DefinitionAccessor!.Operations,
                x => Assert.Equal(1, x.Id),
                x => Assert.Equal(2, x.Id));
        }

        [Fact]
        public void Merge_Should_Merge_Bindings()
        {
            // arrange
            var convention = new MockFilterConvention(
                x => x.BindRuntimeType<int, ComparableOperationFilterInput<int>>());
            var extension = new FilterConventionExtension(
                x => x.BindRuntimeType<double, ComparableOperationFilterInput<double>>());
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
            Assert.Contains(typeof(int), convention.DefinitionAccessor!.Bindings);
            Assert.Contains(typeof(double), convention.DefinitionAccessor!.Bindings);
        }

        [Fact]
        public void Merge_Should_DeepMerge_Configurations()
        {
            // arrange
            var convention = new MockFilterConvention(
                x => x.Configure<ComparableOperationFilterInput<int>>(d => d.Name("Foo")));
            var extension = new FilterConventionExtension(
                x => x.Configure<ComparableOperationFilterInput<int>>(d => d.Name("Bar")));
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
            List<ConfigureFilterInputType> configuration =
                Assert.Single(convention.DefinitionAccessor!.Configurations.Values)!;
            Assert.Equal(2, configuration.Count);
        }

        [Fact]
        public void Merge_Should_Merge_Configurations()
        {
            // arrange
            var convention = new MockFilterConvention(
                x => x.Configure<ComparableOperationFilterInput<int>>(d => d.Name("Foo")));
            var extension = new FilterConventionExtension(
                x => x.Configure<ComparableOperationFilterInput<double>>(d => d.Name("Bar")));
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
            Assert.Equal(2, convention.DefinitionAccessor!.Configurations.Count);
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
            Assert.NotNull(convention.DefinitionAccessor);
            Assert.Equal(2, convention.DefinitionAccessor!.ProviderExtensionsTypes.Count);
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
            Assert.NotNull(convention.DefinitionAccessor);
            Assert.Collection(
                convention.DefinitionAccessor!.ProviderExtensions,
                x => Assert.Equal(provider1, x),
                x => Assert.Equal(provider2, x));
        }

        private class MockProviderExtensions : FilterProviderExtensions<QueryableFilterContext>
        {
        }

        private class MockProvider : IFilterProvider
        {
            public IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; } = null!;

            public FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
            {
                throw new NotImplementedException();
            }

            public void ConfigureField(NameString argumentName, IObjectFieldDescriptor descriptor)
            {
                throw new NotImplementedException();
            }
        }

        private class MockFilterConvention : FilterConvention
        {
            public MockFilterConvention(
                Action<IFilterConventionDescriptor> configure)
                : base(configure)
            {
            }

            public FilterConventionDefinition? DefinitionAccessor => base.Definition;
        }
    }
}
