using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Configuration
{
    public class TypeDiscovererTests
    {
        private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

        [Fact]
        public void Register_SchemaType_ClrTypeExists_NoSystemTypes()
        {
            // arrange
            var typeRegistry = new TypeRegistry();

            var typeDiscoverer = new TypeDiscoverer(
                typeRegistry,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                DescriptorContext.Create(),
                new AggregateTypeInitializationInterceptor(),
                false);

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => t.Type)
                    .OfType<IHasRuntimeType>()
                    .ToDictionary(
                        t => t.GetType().GetTypeName(),
                        t => t.RuntimeType.GetTypeName()),
                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())
            }.MatchSnapshot();
        }

        [Fact]
        public void Register_SchemaType_ClrTypeExists()
        {
            // arrange
            var typeRegistry = new TypeRegistry();

            var typeDiscoverer = new TypeDiscoverer(
                typeRegistry,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                DescriptorContext.Create(),
                new AggregateTypeInitializationInterceptor());

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => t.Type)
                    .OfType<IHasRuntimeType>()
                    .ToDictionary(
                        t => t.GetType().GetTypeName(),
                        t => t.RuntimeType.GetTypeName()),
                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())
            }.MatchSnapshot();
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            var initialTypes = new HashSet<ITypeReference>
            {
                _typeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
            };

            var serviceProvider = new EmptyServiceProvider();

            var clrTypeReferences = new Dictionary<ExtendedTypeReference, ITypeReference>();

            var typeDiscoverer = new TypeDiscoverer(
                initialTypes,
                clrTypeReferences,
                DescriptorContext.Create(),
                new AggregateTypeInitializationInterceptor(),
                serviceProvider);

            // act
            DiscoveredTypes result = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(result.Errors);

            new
            {
                registered = result.Types
                    .Select(t => t.Type)
                    .OfType<IHasRuntimeType>()
                    .ToDictionary(
                        t => t.GetType().GetTypeName(),
                        t => t.RuntimeType.GetTypeName()),
                clr = clrTypeReferences.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString())
            }.MatchSnapshot();
        }

        [Fact]
        public void Upgrade_Type_From_GenericType()
        {
            // arrange
            var initialTypes = new HashSet<ITypeReference>
            {
                _typeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
            };

            var serviceProvider = new EmptyServiceProvider();

            var clrTypeReferences = new Dictionary<ExtendedTypeReference, ITypeReference>();

            var typeDiscoverer = new TypeDiscoverer(
                initialTypes,
                clrTypeReferences,
                DescriptorContext.Create(),
                new AggregateTypeInitializationInterceptor(),
                serviceProvider);

            // act
            DiscoveredTypes result = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(result.Errors);

            new
            {
                registered = result.Types
                    .Select(t => t.Type)
                    .OfType<IHasRuntimeType>()
                    .ToDictionary(
                        t => t.GetType().GetTypeName(),
                        t => t.RuntimeType.GetTypeName()),
                clr = clrTypeReferences.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())
            }.MatchSnapshot();
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
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
