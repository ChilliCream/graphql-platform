#nullable disable

using System.Numerics;
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
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
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
                    runtimeType = t.Type is IRuntimeTypeProvider hr
                        ? hr.RuntimeType.GetTypeName()
                        : null,
                    references = t.References.Select(r => r.ToString()).ToList()
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
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
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
                    runtimeType = t.Type is IRuntimeTypeProvider hr
                        ? hr.RuntimeType.GetTypeName()
                        : null,
                    references = t.References.Select(r => r.ToString()).ToList()
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
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(Foo), TypeContext.Output)
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
                    runtimeType = t.Type is IRuntimeTypeProvider hr
                        ? hr.RuntimeType.GetTypeName()
                        : null,
                    references = t.References.ConvertAll(r => r.ToString())
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
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);

        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(ObjectType<Foo>), TypeContext.Output),
                _typeInspector.GetTypeRef(typeof(FooType), TypeContext.Output)
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
                    runtimeType = t.Type is IRuntimeTypeProvider hr
                        ? hr.RuntimeType.GetTypeName()
                        : null,
                    references = t.References.Select(r => r.ToString()).ToList()
                }).ToList(),

            runtimeTypeRefs = typeRegistry.RuntimeTypeRefs.ToDictionary(
                t => t.Key.ToString(),
                t => t.Value.ToString())

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
                _typeInspector.GetTypeRef(typeof(QueryWithInferError), TypeContext.Output)
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
                _typeInspector.GetTypeRef(typeof(QueryWithInferError2), TypeContext.Output)
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

    [Fact]
    public void Can_Infer_Generic_Record_Struct_Input_Type()
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
                _typeInspector.GetTypeRef(
                    typeof(QueryWithGenericRecordStructInput),
                    TypeContext.Output)
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void DiscoverTypes_Should_Append_PathToRoot_When_Nested_Member_Cannot_Be_Inferred()
    {
        // arrange
        // the query reaches an un-inferable interface through two nested members.
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(NestedQuery), TypeContext.Output)
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

        // assert
        var error = Assert.Single(errors);
        Assert.Contains(" -> ", error.Message, StringComparison.Ordinal);
        new SchemaException(errors).Message.MatchSnapshot();
    }

    [Fact]
    public void DiscoverTypes_Should_Expose_TypePath_Extension_When_Member_Cannot_Be_Inferred()
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
                _typeInspector.GetTypeRef(typeof(NestedQuery), TypeContext.Output)
            },
            new AggregateTypeInterceptor());

        // act
        var errors = typeDiscoverer.DiscoverTypes();

        // assert
        // the message keeps the short form while the extension is namespace-qualified.
        var error = Assert.Single(errors);
        Assert.Contains(
            "NestedQuery.GetFoo -> FooWithBar.Bar -> BarWithBaz.Baz(input) -> IMyArg",
            error.Message,
            StringComparison.Ordinal);
        Assert.Equal(
            "HotChocolate.Configuration.NestedQuery.GetFoo "
                + "-> HotChocolate.Configuration.FooWithBar.Bar "
                + "-> HotChocolate.Configuration.BarWithBaz.Baz(input) "
                + "-> HotChocolate.Configuration.IMyArg",
            Assert.IsType<string>(error.Extensions[TypeErrorFields.Path]));
    }

    [Fact]
    public void Build_Should_Use_Friendly_GenericName_When_Leaf_Is_ReadOnlyMemory()
    {
        // arrange
        // ReadOnlyMemory<byte> resolves during discovery, so the path is built
        // directly to demonstrate the friendly leaf name formatting.
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(MemoryQuery), TypeContext.Output)
            },
            new AggregateTypeInterceptor());
        typeDiscoverer.DiscoverTypes();

        var reference = _typeInspector.GetTypeRef(typeof(ReadOnlyMemory<byte>), TypeContext.Input);

        // act
        var path = TypeInferencePathBuilder.Build(typeRegistry, reference);

        // assert
        // the short form uses aliases without namespaces, the expanded form qualifies them.
        Assert.Equal("MemoryQuery.Baz(input) -> ReadOnlyMemory<byte>", path?.Short);
        Assert.Equal(
            "HotChocolate.Configuration.MemoryQuery.Baz(input) -> System.ReadOnlyMemory<byte>",
            path?.Expanded);
    }

    [Fact]
    public void Build_Should_Truncate_With_Marker_When_Chain_Exceeds_Cap()
    {
        // arrange
        // the chain reaches the un-inferable leaf through more hops than the cap allows.
        var context = DescriptorContext.Create();
        var typeRegistry = new TypeRegistry(context.TypeInterceptor);
        var typeLookup = new TypeLookup(context.TypeInspector, typeRegistry);

        var typeDiscoverer = new TypeDiscoverer(
            context,
            typeRegistry,
            typeLookup,
            new HashSet<TypeReference>
            {
                _typeInspector.GetTypeRef(typeof(DeepQuery), TypeContext.Output)
            },
            new AggregateTypeInterceptor());
        typeDiscoverer.DiscoverTypes();

        var reference = _typeInspector.GetTypeRef(typeof(IMyArg), TypeContext.Input);

        // act
        var path = TypeInferencePathBuilder.Build(typeRegistry, reference);

        // assert
        // both renderings share the same truncation, so the part count is identical.
        var shortParts = path!.Value.Short.Split(" -> ");
        var expandedParts = path.Value.Expanded.Split(" -> ");
        Assert.Equal(5, shortParts.Length);
        Assert.Equal("...", shortParts[0]);
        Assert.Equal("IMyArg", shortParts[^1]);
        Assert.Equal(shortParts.Length, expandedParts.Length);
    }

    public class DeepQuery
    {
        public DeepLevel1 GetLevel1() => throw new NotImplementedException();
    }

    public class DeepLevel1
    {
        public DeepLevel2 Level2 { get; } = null!;
    }

    public class DeepLevel2
    {
        public DeepLevel3 Level3 { get; } = null!;
    }

    public class DeepLevel3
    {
        public DeepLevel4 Level4 { get; } = null!;
    }

    public class DeepLevel4
    {
        public DeepLevel5 Level5 { get; } = null!;
    }

    public class DeepLevel5
    {
        public DeepLevel6 Level6 { get; } = null!;
    }

    public class DeepLevel6
    {
        public string Leaf(IMyArg arg) => throw new NotImplementedException();
    }

    public class NestedQuery
    {
        public FooWithBar GetFoo() => throw new NotImplementedException();
    }

    public class FooWithBar
    {
        public BarWithBaz Bar { get; } = null!;
    }

    public class BarWithBaz
    {
        public string Baz(IMyArg input) => throw new NotImplementedException();
    }

    public class MemoryQuery
    {
        public string Baz(ReadOnlyMemory<byte> input) => throw new NotImplementedException();
    }

    public class FooType
        : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar).Type<NonNullType<BarType>>();
        }
    }

    public class BarType : ObjectType<Bar>;

    public class Foo(Bar bar)
    {
        public Bar Bar { get; } = bar;
    }

    public class Bar(string baz)
    {
        public string Baz { get; } = baz;
    }

    public class QueryWithInferError
    {
        public string Foo(IMyArg o) => throw new NotImplementedException();
    }

    public class QueryWithInferError2
    {
        public string Foo(IMyArg o) => throw new NotImplementedException();
    }

    public class QueryWithGenericRecordStructInput
    {
        public string Foo(RangeSpec<int> range) => throw new NotImplementedException();
    }

    public readonly record struct RangeSpec<T>(
        T? Min,
        T? Max)
        where T : struct, IComparable<T>, IComparisonOperators<T, T, bool>;

    public interface IMyArg;
}
