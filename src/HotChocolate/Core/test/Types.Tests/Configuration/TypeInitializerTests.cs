using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeInitializerTests
    {
        [Fact]
        public void Register_SchemaType_ClrTypeExists()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(TypeReference.Create(
                typeof(FooType),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                DescriptorContext.Create(),
                initialTypes,
                new List<Type>(),
                new AggregateTypeInitializationInterceptor(),
                null,
                t => t is FooType);

            // act
            typeInitializer.Initialize(() => null, new SchemaOptions());

            // assert
            bool exists = typeInitializer.DiscoveredTypes.TryGetType(
                TypeReference.Create(typeof(FooType), TypeContext.Output),
                out RegisteredType type);

            Assert.True(exists);
            Dictionary<string, string> fooType =
                Assert.IsType<FooType>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => TypeVisualizer.Visualize(t.Type));

            exists = typeInitializer.DiscoveredTypes.TryGetType(
                TypeReference.Create(typeof(BarType), TypeContext.Output),
                out type);

            Assert.True(exists);
            Dictionary<string, string> barType =
                Assert.IsType<BarType>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => TypeVisualizer.Visualize(t.Type));

            new { fooType, barType }.MatchSnapshot();
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(TypeReference.Create(
                typeof(Foo),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                DescriptorContext.Create(),
                initialTypes,
                new List<Type>(),
                new AggregateTypeInitializationInterceptor(),
                null,
                t => t is ObjectType<Foo>);

            // act
            typeInitializer.Initialize(() => null, new SchemaOptions());

            // assert
            bool exists = typeInitializer.DiscoveredTypes.TryGetType(
                TypeReference.Create(
                    typeof(ObjectType<Foo>),
                    TypeContext.Output),
                out RegisteredType type);

            Assert.True(exists);
            Dictionary<string, string> fooType =
                Assert.IsType<ObjectType<Foo>>(type.Type).Fields.ToDictionary(
                t => t.Name.ToString(),
                t => TypeVisualizer.Visualize(t.Type));

            exists = typeInitializer.DiscoveredTypes.TryGetType(
                TypeReference.Create(typeof(ObjectType<Bar>), TypeContext.Output),
                out type);

            Assert.True(exists);
            Dictionary<string, string> barType =
                Assert.IsType<ObjectType<Bar>>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => TypeVisualizer.Visualize(t.Type));

            new { fooType, barType }.MatchSnapshot();
        }

        [Fact]
        public void Initializer_SchemaResolver_Is_Null()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(TypeReference.Create(
                typeof(Foo),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                DescriptorContext.Create(),
                initialTypes,
                new List<Type>(),
                new AggregateTypeInitializationInterceptor(),
                null,
                t => t is ObjectType<Foo>);

            // act
            Action action =
                () => typeInitializer.Initialize(null, new SchemaOptions());

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Initializer_SchemaOptions_Are_Null()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(TypeReference.Create(
                typeof(Foo),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                DescriptorContext.Create(),
                initialTypes,
                new List<Type>(),
                new AggregateTypeInitializationInterceptor(),
                null,
                t => t is ObjectType<Foo>);

            // act
            Action action =
                () => typeInitializer.Initialize(() => null, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
            }
        }

        public class BarType
            : ObjectType<Bar>
        {
        }

        public class Foo
        {
            public Bar Bar { get; }
        }

        public class Bar
        {
            public string Baz { get; }
        }
    }
}