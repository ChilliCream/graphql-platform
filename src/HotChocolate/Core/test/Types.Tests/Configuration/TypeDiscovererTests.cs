using System.Collections.Generic;
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
            var context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();
            var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

            var typeDiscoverer = new TypeDiscoverer(
                context,
                typeRegistry,
                typeLookup,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                new AggregateTypeInitializationInterceptor(),
                false);

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => new
                    {
                        type = t.Type.GetType().GetTypeName(),
                        runtimeType = t.Type is IHasRuntimeType hr 
                            ? hr.RuntimeType.GetTypeName() 
                            : null,
                        references = t.References.Select(t => t.ToString()).ToList()
                    }).ToList(),

                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())

            }.MatchSnapshot();
        }

        [Fact]
        public void Register_SchemaType_ClrTypeExists()
        {
            // arrange
            var context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();
            var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

            var typeDiscoverer = new TypeDiscoverer(
                context,
                typeRegistry,
                typeLookup,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                new AggregateTypeInitializationInterceptor());

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => new
                    {
                        type = t.Type.GetType().GetTypeName(),
                        runtimeType = t.Type is IHasRuntimeType hr 
                            ? hr.RuntimeType.GetTypeName() 
                            : null,
                        references = t.References.Select(t => t.ToString()).ToList()
                    }).ToList(),

                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())

            }.MatchSnapshot();
        }

        [Fact]
        public void Register_ClrType_InferSchemaTypes()
        {
            // arrange
            var context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();
            var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

            var typeDiscoverer = new TypeDiscoverer(
                context,
                typeRegistry,
                typeLookup,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new AggregateTypeInitializationInterceptor());

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => new
                    {
                        type = t.Type.GetType().GetTypeName(),
                        runtimeType = t.Type is IHasRuntimeType hr 
                            ? hr.RuntimeType.GetTypeName() 
                            : null,
                        references = t.References.Select(t => t.ToString()).ToList()
                    }).ToList(),

                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                    t => t.Key.ToString(),
                    t => t.Value.ToString())

            }.MatchSnapshot();
        }

        [Fact]
        public void Upgrade_Type_From_GenericType()
        {
            // arrange
            var context = DescriptorContext.Create();
            var typeRegistry = new TypeRegistry();
            var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

            var typeDiscoverer = new TypeDiscoverer(
                context,
                typeRegistry,
                typeLookup,
                new HashSet<ITypeReference>
                {
                    _typeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
                    _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                new AggregateTypeInitializationInterceptor());

            // act
            IReadOnlyList<ISchemaError> errors = typeDiscoverer.DiscoverTypes();

            // assert
            Assert.Empty(errors);

            new
            {
                registered = typeRegistry.Types
                    .Select(t => new
                    {
                        type = t.Type.GetType().GetTypeName(),
                        runtimeType = t.Type is IHasRuntimeType hr 
                            ? hr.RuntimeType.GetTypeName() 
                            : null,
                        references = t.References.Select(t => t.ToString()).ToList()
                    }).ToList(),

                runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
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
