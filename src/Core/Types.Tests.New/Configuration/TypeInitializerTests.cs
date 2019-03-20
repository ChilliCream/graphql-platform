using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate
{
    public class TypeInitializerTests
    {
        [Fact]
        public void Register_SchemaType_ClrTypeExists()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(new ClrTypeReference(
                typeof(FooType),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                initialTypes,
                t => t is FooType);

            // act
            typeInitializer.Initialize();

            // assert
            Assert.Collection(typeInitializer.Types,
                t => Assert.Equal(typeof(Foo),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(Bar),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(string),
                    ((IHasClrType)t.Value.Type).ClrType));
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            var initialTypes = new List<ITypeReference>();
            initialTypes.Add(new ClrTypeReference(
                typeof(Foo),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var typeInitializer = new TypeInitializer(
                serviceProvider,
                initialTypes,
                t => t is ObjectType<Foo>);

            // act
            typeInitializer.Initialize();

            // assert
            Assert.Collection(typeInitializer.Types,
                t => Assert.Equal(typeof(Foo),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(Bar),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(string),
                    ((IHasClrType)t.Value.Type).ClrType));
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
