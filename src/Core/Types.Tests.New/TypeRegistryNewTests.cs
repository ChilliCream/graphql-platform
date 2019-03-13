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
        public void Test123()
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
            Assert.Empty(typeRegistrar.ClrTypes);

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
