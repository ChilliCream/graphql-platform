using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;
using Xunit;

namespace HotChocolate
{
    public class TypeRegistrarTests
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

            var typeRegistrar = new TypeRegistrar_new(
                initialTypes,
                serviceProvider);

            // act
            typeRegistrar.Complete();

            // assert
            Assert.Collection(typeRegistrar.Registerd,
                t => Assert.Equal(typeof(Foo),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(Bar),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(string),
                    ((IHasClrType)t.Value.Type).ClrType));

            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(Foo), TypeContext.Output)));
            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(Bar), TypeContext.Output)));
            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(string), TypeContext.None)));
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

            var typeRegistrar = new TypeRegistrar_new(
                initialTypes,
                serviceProvider);

            // act
            typeRegistrar.Complete();

            // assert
            Assert.Collection(typeRegistrar.Registerd,
                t => Assert.Equal(typeof(Foo),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(Bar),
                    ((IHasClrType)t.Value.Type).ClrType),
                t => Assert.Equal(typeof(string),
                    ((IHasClrType)t.Value.Type).ClrType));

            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(Foo), TypeContext.Output)));
            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(Bar), TypeContext.Output)));
            Assert.True(typeRegistrar.ClrTypes.ContainsKey(
                new ClrTypeReference(typeof(string), TypeContext.None)));
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
