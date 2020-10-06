using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Conventions;
using HotChocolate.Types.Introspection;
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
            IDescriptorContext context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                new List<Type>(),
                new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()),
                null,
                t => t is FooType);

            // act
            typeInitializer.Initialize(() => null, new SchemaOptions());

            // assert
            var exists = typeRegistry.TryGetType(
                context.TypeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
                out RegisteredType type);

            Assert.True(exists);
            var fooType =
                Assert.IsType<FooType>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => t.Type.Print());

            exists = typeRegistry.TryGetType(
                context.TypeInspector.GetTypeRef(typeof(BarType), TypeContext.Output),
                out type);

            Assert.True(exists);
            var barType =
                Assert.IsType<BarType>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => t.Type.Print());

            new { fooType, barType }.MatchSnapshot();
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            IDescriptorContext context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()),
                null,
                t => t is ObjectType<Foo>);

            // act
            typeInitializer.Initialize(() => null, new SchemaOptions());

            // assert
            var exists = typeRegistry.TryGetType(
                context.TypeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
                out RegisteredType type);

            Assert.True(exists);
            var fooType =
                Assert.IsType<ObjectType<Foo>>(type.Type).Fields.ToDictionary(
                t => t.Name.ToString(),
                t => t.Type.Print());

            exists = typeRegistry.TryGetType(
                context.TypeInspector.GetTypeRef(typeof(ObjectType<Bar>), TypeContext.Output),
                out type);

            Assert.True(exists);
            var barType =
                Assert.IsType<ObjectType<Bar>>(type.Type).Fields.ToDictionary(
                    t => t.Name.ToString(),
                    t => t.Type.Print());

            new { fooType, barType }.MatchSnapshot();
        }

        [Fact]
        public void Initializer_SchemaResolver_Is_Null()
        {
            // arrange
            IDescriptorContext context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()),
                null!,
                t => t is ObjectType<Foo>);

            // act
            void Action() => typeInitializer.Initialize(null!, new SchemaOptions());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Initializer_SchemaOptions_Are_Null()
        {
            // arrange
            IDescriptorContext context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()),
                null!,
                t => t is ObjectType<Foo>);

            // act
            void Action() => typeInitializer.Initialize(() => null, null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
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
