using System.Linq;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    public class TypeRegistrarTests
    {
        [Fact]
        public void Register_SchemaType_ClrTypeExists()
        {
            // arrange
            var initialTypes = new HashSet<ITypeReference>();
            initialTypes.Add(new ClrTypeReference(
                typeof(FooType),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var clrTypeReferences = new Dictionary<IClrTypeReference, ITypeReference>();

            var typeDiscoverer = new TypeDiscoverer(
                initialTypes,
                clrTypeReferences,
                DescriptorContext.Create(),
                new Dictionary<string, object>(),
                serviceProvider);

            // act
            DiscoveredTypes result = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(result.Errors);

            result.Types
                .Select(t => t.Type)
                .OfType<IHasClrType>()
                .ToDictionary(
                    t => t.GetType().GetTypeName(),
                    t => t.ClrType.GetTypeName())
                .MatchSnapshot(new SnapshotNameExtension("registered"));

            clrTypeReferences.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString())
                .MatchSnapshot(new SnapshotNameExtension("clr"));
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            var initialTypes = new HashSet<ITypeReference>();
            initialTypes.Add(new ClrTypeReference(
                typeof(Foo),
                TypeContext.Output));

            var serviceProvider = new EmptyServiceProvider();

            var clrTypeReferences = new Dictionary<IClrTypeReference, ITypeReference>();

            var typeDiscoverer = new TypeDiscoverer(
                initialTypes,
                clrTypeReferences,
                DescriptorContext.Create(),
                new Dictionary<string, object>(),
                serviceProvider);

            // act
            DiscoveredTypes result = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(result.Errors);

            result.Types
                .Select(t => t.Type)
                .OfType<IHasClrType>()
                .ToDictionary(
                    t => t.GetType().GetTypeName(),
                    t => t.ClrType.GetTypeName())
                .MatchSnapshot(new SnapshotNameExtension("registered"));

            clrTypeReferences.ToDictionary(
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
