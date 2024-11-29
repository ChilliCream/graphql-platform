using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration;

public class TypeDiscovererTests
{
    private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

    [Fact]
    public void Register_SchemaType_ClrTypeExists_NoSystemTypes()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
            },
            new AggregateTypeInterceptor(),
            false);

        // act
        var errors = typeDiscoverer.DiscoverTypes();

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
                    references = t.References.Select(r => r.ToString()).ToList(),
                }).ToList(),

            runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString()),

        }.MatchSnapshot();
    }

    [Fact]
    public void Register_SchemaType_ClrTypeExists()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

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
                    references = t.References.Select(r => r.ToString()).ToList(),
                }).ToList(),

            runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString()),

        }.MatchSnapshot();
    }

    [Fact]
    public void Register_ClrType_InferSchemaTypes()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(Foo), TypeContext.Output),
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

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
                    references = t.References.Select(r => r.ToString()).ToList(),
                }).ToList(),

            runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString()),

        }.MatchSnapshot();
    }

    [Fact]
    public void Upgrade_Type_From_GenericType()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output),
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

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
                    references = t.References.Select(r => r.ToString()).ToList(),
                }).ToList(),

            runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString()),

        }.MatchSnapshot();
    }

    [Fact]
    public void Cannot_Infer_Input_Type()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(QueryWithInferError), TypeContext.Output),
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

        // assert
        Assert.Collection(
            errors,
            error =>
            {
                Assert.Equal(ErrorCodes.Schema.UnresolvedTypes, error.Code);
                Assert.IsType<ObjectType<QueryWithInferError>>(error.TypeSystemObject);
                Assert.False(error.Extensions.ContainsKey("involvedTypes"));
            });

        new SchemaException(errors).Message.MatchSnapshot();
    }

    [Fact]
    public void Cannot_Infer_Multiple_Input_Type()
    {
        // arrange
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(QueryWithInferError), TypeContext.Output),
                _typeInspector.GetTypeRef(typeof(QueryWithInferError2), TypeContext.Output),
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

        // assert
        Assert.Collection(
            errors,
            error =>
            {
                Assert.Equal(ErrorCodes.Schema.UnresolvedTypes, error.Code);
                Assert.IsType<ObjectType<QueryWithInferError>>(error.TypeSystemObject);
                Assert.True(error.Extensions.ContainsKey("involvedTypes"));
            });
    }

    public class FooType
        : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
        }
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

    public class QueryWithInferError
    {
        public string Foo(IMyArg o) => throw new NotImplementedException();
    }

    public class QueryWithInferError2
    {
        public string Foo(IMyArg o) => throw new NotImplementedException();
    }

    public interface IMyArg;
}
