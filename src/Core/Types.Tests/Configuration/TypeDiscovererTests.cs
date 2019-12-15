using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeDiscovererTests
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

        [Fact]
        public void Upgrade_Type_From_GenericType()
        {
            // arrange
            var initialTypes = new HashSet<ITypeReference>();
            initialTypes.Add(new ClrTypeReference(
                typeof(ObjectType<Foo>),
                TypeContext.Output));
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
