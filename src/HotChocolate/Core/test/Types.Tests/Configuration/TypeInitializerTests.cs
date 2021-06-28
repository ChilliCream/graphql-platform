using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
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
            IDescriptorContext context = DescriptorContext.Create(
                typeInterceptor: new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()));
            var typeRegistry = new TypeRegistry(context.TypeInterceptor);

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
                },
                new List<Type>(),
                null,
                t => t is FooType ? RootTypeKind.Query : RootTypeKind.None);

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
            IDescriptorContext context = DescriptorContext.Create(
                typeInterceptor: new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()));
            var typeRegistry = new TypeRegistry(context.TypeInterceptor);

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                null,
                t =>
                {
                    return t switch
                    {
                        ObjectType<Foo> => RootTypeKind.Query,
                        _ => RootTypeKind.None
                    };
                });

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
            IDescriptorContext context = DescriptorContext.Create(
                typeInterceptor: new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()));
            var typeRegistry = new TypeRegistry(context.TypeInterceptor);

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                null!,
                t =>
                {
                    return t switch
                    {
                        ObjectType<Foo> => RootTypeKind.Query,
                        _ => RootTypeKind.None
                    };
                });

            // act
            void Action() => typeInitializer.Initialize(null!, new SchemaOptions());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Initializer_SchemaOptions_Are_Null()
        {
            // arrange
            IDescriptorContext context = DescriptorContext.Create(
                typeInterceptor: new AggregateTypeInterceptor(new IntrospectionTypeInterceptor()));
            var typeRegistry = new TypeRegistry(context.TypeInterceptor);

            var typeInitializer = new TypeInitializer(
                context,
                typeRegistry,
                new List<ITypeReference>
                {
                    context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
                },
                new List<Type>(),
                null!,
                t =>
                {
                    return t switch
                    {
                        ObjectType<Foo> => RootTypeKind.Query,
                        _ => RootTypeKind.None
                    };
                });

            // act
            void Action() => typeInitializer.Initialize(() => null, null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Detect_Duplicate_Types()
        {
            // arrange
            var type = new ObjectType(d => d.Name("Abc").Field("def").Resolve("ghi"));

            // the interceptor will add multiple types references to type and count
            // how many times the type is registered.
            var interceptor = new TypeRegInterceptor(type);

            // act
            SchemaBuilder.New()
                .AddQueryType(type)
                .TryAddTypeInterceptor(interceptor)
                .Create();

            // assert
            Assert.Equal(1, interceptor.Count);
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

        private class TypeRegInterceptor : TypeInterceptor
        {
            private readonly IType _watch;

            public TypeRegInterceptor(IType watch)
            {
                _watch = watch;
            }

            public int Count { get; private set; }

            public override void OnBeforeInitialize(ITypeDiscoveryContext discoveryContext)
            {
                if (!ReferenceEquals(_watch, discoveryContext.Type))
                {
                    discoveryContext.RegisterDependency(
                        new TypeDependency(TypeReference.Create(_watch)));

                    discoveryContext.RegisterDependency(
                        new TypeDependency(TypeReference.Create(new ListType(_watch))));
                }
            }

            public override void OnTypeRegistered(ITypeDiscoveryContext discoveryContext)
            {
                if (ReferenceEquals(_watch, discoveryContext.Type))
                {
                    Count++;
                }
            }
        }
    }
}
