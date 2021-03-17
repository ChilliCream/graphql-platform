using System;
using System.Collections.Generic;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Data
{
    public class SortConventionExtensionsTests
    {
        [Fact]
        public void Merge_Should_Merge_ArgumentName()
        {
            // arrange
            var convention = new MockSortConvention(x => x.ArgumentName("Foo"));
            var extension = new SortConventionExtension(x => x.ArgumentName("Bar"));
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
            var convention = new MockSortConvention(x => x.ArgumentName("Foo"));
            var extension = new SortConventionExtension(
                x => x.ArgumentName(SortConventionDefinition.DefaultArgumentName));
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
            var convention = new MockSortConvention(x => x.Provider<QueryableSortProvider>());
            var extension = new SortConventionExtension(x => x.Provider<MockProvider>());
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
            var convention = new MockSortConvention(
                x => x.Provider(new QueryableSortProvider()));
            var extension = new SortConventionExtension(
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
        public void Merge_Should_Merge_DefaultBinding()
        {
            // arrange
            var convention = new MockSortConvention(
                x => x.DefaultBinding<DefaultSortEnumType>());
            var extension = new SortConventionExtension(
                x => x.DefaultBinding<MockSortEnumType>());
            var context = new ConventionContext(
                "Scope",
                new ServiceCollection().BuildServiceProvider(),
                DescriptorContext.Create());

            convention.Initialize(context);
            extension.Initialize(context);

            // act
            extension.Merge(context, convention);

            // assert
            Assert.Equal(
                typeof(MockSortEnumType),
                convention.DefinitionAccessor?.DefaultBinding);
        }

        [Fact]
        public void Merge_Should_Merge_Operations()
        {
            // arrange
            var convention = new MockSortConvention(x => x.Operation(1));
            var extension = new SortConventionExtension(x => x.Operation(2));
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
            var convention = new MockSortConvention(
                x => x.BindRuntimeType<int, DefaultSortEnumType>());
            var extension = new SortConventionExtension(
                x => x.BindRuntimeType<double, DefaultSortEnumType>());
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
            var convention = new MockSortConvention(
                x => x.Configure<SortInputType<Foo>>(d => d.Name("Foo")));
            var extension = new SortConventionExtension(
                x => x.Configure<SortInputType<Foo>>(d => d.Name("Bar")));
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
            List<ConfigureSortInputType> configuration =
                Assert.Single(convention.DefinitionAccessor!.Configurations.Values)!;
            Assert.Equal(2, configuration.Count);
        }

        [Fact]
        public void Merge_Should_Merge_Configurations()
        {
            // arrange
            var convention = new MockSortConvention(
                x => x.Configure<SortInputType<Foo>>(d => d.Name("Foo")));
            var extension = new SortConventionExtension(
                x => x.Configure<SortInputType<Bar>>(d => d.Name("Foo")));
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
        public void Merge_Should_DeepMerge_EnumConfigurations()
        {
            // arrange
            var convention = new MockSortConvention(
                x => x.ConfigureEnum<DefaultSortEnumType>(d => d.Name("Foo")));
            var extension = new SortConventionExtension(
                x => x.ConfigureEnum<DefaultSortEnumType>(d => d.Name("Foo")));
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
            List<ConfigureSortEnumType> configuration =
                Assert.Single(convention.DefinitionAccessor!.EnumConfigurations.Values)!;
            Assert.Equal(2, configuration.Count);
        }

        [Fact]
        public void Merge_Should_Merge_EnumConfigurations()
        {
            // arrange
            var convention = new MockSortConvention(
                x => x.ConfigureEnum<DefaultSortEnumType>(d => d.Name("Foo")));
            var extension = new SortConventionExtension(
                x => x.ConfigureEnum<MockSortEnumType>(d => d.Name("Foo")));
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
            Assert.Equal(2, convention.DefinitionAccessor!.EnumConfigurations.Count);
        }

        [Fact]
        public void Merge_Should_Merge_ProviderExtensionsTypes()
        {
            // arrange
            var convention =
                new MockSortConvention(x => x.AddProviderExtension<MockProviderExtensions>());
            var extension =
                new SortConventionExtension(
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
            var convention = new MockSortConvention(x => x.AddProviderExtension(provider1));
            var provider2 = new MockProviderExtensions();
            var extension = new SortConventionExtension(x => x.AddProviderExtension(provider2));
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

        private class Foo
        {
            public string Bar { get; }
        }

        private class Bar
        {
            public string Foo { get; }
        }

        private class MockSortEnumType : DefaultSortEnumType
        {
        }

        private class MockProviderExtensions : SortProviderExtensions<QueryableSortContext>
        {
        }

        private class MockProvider : ISortProvider
        {
            public IReadOnlyCollection<ISortFieldHandler> FieldHandlers { get; } = null!;
            public IReadOnlyCollection<ISortOperationHandler> OperationHandlers { get; } = null!;

            public FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName)
            {
                throw new NotImplementedException();
            }

            public void ConfigureField(NameString argumentName, IObjectFieldDescriptor descriptor)
            {
                throw new NotImplementedException();
            }
        }

        private class MockSortConvention : SortConvention
        {
            public MockSortConvention(
                Action<ISortConventionDescriptor> configure)
                : base(configure)
            {
            }

            public SortConventionDefinition? DefinitionAccessor => base.Definition;
        }
    }
}
