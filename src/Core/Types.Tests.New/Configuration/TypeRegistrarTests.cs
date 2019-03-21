using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Moq;
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;

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
                serviceProvider, initialTypes);

            // act
            typeRegistrar.Complete();

            // assert
            typeRegistrar.Registerd
                .Select(t => t.Value.Type)
                .OfType<IHasClrType>()
                .ToDictionary(
                    t => t.GetType().FullName,
                    t => t.ClrType.FullName)
                .MatchSnapshot(new SnapshotNameExtension("registered"));

            typeRegistrar.ClrTypes.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString())
                .MatchSnapshot(new SnapshotNameExtension("clr"));
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
                serviceProvider, initialTypes);

            // act
            typeRegistrar.Complete();

            // assert
            typeRegistrar.Registerd
                .Select(t => t.Value.Type)
                .OfType<IHasClrType>()
                .ToDictionary(
                    t => t.GetType().FullName,
                    t => t.ClrType.FullName)
                .MatchSnapshot(new SnapshotNameExtension("registered"));

            typeRegistrar.ClrTypes.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString())
                .MatchSnapshot(new SnapshotNameExtension("clr"));
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
