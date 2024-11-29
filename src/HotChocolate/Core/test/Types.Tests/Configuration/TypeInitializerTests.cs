using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Configuration;

public class TypeInitializerTests
{
    [Fact]
    public void Register_SchemaType_ClrTypeExists()
    {
        // arrange
        var typeInterceptor = new AggregateTypeInterceptor();
        typeInterceptor.SetInterceptors(new[] { new IntrospectionTypeInterceptor(), });
        IDescriptorContext context = DescriptorContext.Create(
            typeInterceptor: typeInterceptor);
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeInitializer = new TypeInitializer(
            context,
            typeRegistry,
            new List<TypeReference>
            {
                context.TypeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
            },
            null,
            t => t is FooType ? RootTypeKind.Query : RootTypeKind.None,
            new SchemaOptions());

        // act
        typeInitializer.Initialize();

        // assert
        var exists = typeRegistry.TryGetType(
            context.TypeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
            out var type);

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

        new { fooType, barType, }.MatchSnapshot();
    }

    [Fact]
    public void Register_ClrType_InferSchemaTypes()
    {
        // arrange
        var typeInterceptor = new AggregateTypeInterceptor();
        typeInterceptor.SetInterceptors(new[] { new IntrospectionTypeInterceptor(), });
        IDescriptorContext context = DescriptorContext.Create(
            typeInterceptor: typeInterceptor);
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeInitializer = new TypeInitializer(
            context,
            typeRegistry,
            new List<TypeReference>
            {
                context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output),
            },
            null,
            t =>
            {
                return t switch
                {
                    ObjectType<Foo> => RootTypeKind.Query,
                    _ => RootTypeKind.None,
                };
            },
            new SchemaOptions());

        // act
        typeInitializer.Initialize();

        // assert
        var exists = typeRegistry.TryGetType(
            context.TypeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
            out var type);

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

        new { fooType, barType, }.MatchSnapshot();
    }

    [Fact]
    public void Initializer_SchemaOptions_Are_Null()
    {
        // arrange
        var typeInterceptor = new AggregateTypeInterceptor();
        typeInterceptor.SetInterceptors(new[] { new IntrospectionTypeInterceptor(), });
        IDescriptorContext context = DescriptorContext.Create(
            typeInterceptor: typeInterceptor);
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        // act
        void Action() => new TypeInitializer(
            context,
            typeRegistry,
            new List<TypeReference>
            {
                context.TypeInspector.GetTypeRef(typeof(Foo), TypeContext.Output),
            },
            null!,
            t =>
            {
                return t switch
                {
                    ObjectType<Foo> => RootTypeKind.Query,
                    _ => RootTypeKind.None,
                };
            },
            null!);

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

    [Fact]
    public void InitializeFactoryTypeRefOnce()
    {
        // arrange
        var typeRef1 = TypeReference.Parse(
            "Abc",
            factory: _ => new ObjectType(d => d.Name("Abc").Field("def").Resolve("ghi")));

        var typeRef2 = TypeReference.Parse(
            "Abc",
            factory: _ => new ObjectType(d => d.Name("Abc").Field("def").Resolve("ghi")));

        var interceptor = new InjectTypes(new[] { typeRef1, typeRef2, });

        // act
        var schema =
            SchemaBuilder.New()
                .TryAddTypeInterceptor(interceptor)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public void FactoryAndNameRefsAreRecognizedAsTheSameType()
    {
        // arrange
        var typeRef1 = TypeReference.Parse(
            "Abc",
            factory: _ => new ObjectType(d => d.Name("Abc").Field("def").Resolve("ghi")));

        var typeRef2 = TypeReference.Parse("Abc");

        var interceptor = new InjectTypes(new[] { typeRef1, typeRef2, });

        // act
        var schema =
            SchemaBuilder.New()
                .TryAddTypeInterceptor(interceptor)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

        // assert
        schema.Print().MatchSnapshot();
    }

    public class FooType : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            => descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
    }

    public class BarType : ObjectType<Bar>
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

    private sealed class TypeRegInterceptor : TypeInterceptor
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
                discoveryContext.Dependencies.Add(
                    new(TypeReference.Create(_watch)));

                discoveryContext.Dependencies.Add(
                    new(TypeReference.Create(new ListType(_watch))));
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

    private sealed class InjectTypes : TypeInterceptor
    {
        private readonly List<TypeReference> _typeReferences;

        public InjectTypes(IEnumerable<TypeReference> typeReferences)
            => _typeReferences = typeReferences.ToList();

        public override IEnumerable<TypeReference> RegisterMoreTypes(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
            => _typeReferences;
    }
}
