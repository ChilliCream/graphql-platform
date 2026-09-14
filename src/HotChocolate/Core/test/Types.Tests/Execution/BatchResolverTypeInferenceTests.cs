using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Tests;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverTypeInferenceTests
{
    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DiscoveredByReflection()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<AttributeBatchQuery>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DeclaredThroughFieldSelector()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FieldSelectorBatchQuery>(d =>
            {
                d.Field(q => q.GetNonNullProducts(default!));
                d.Field(q => q.GetNullableProducts(default!));
                d.Field(q => q.GetForcedNonNullProducts(default!));
                d.Field(q => q.GetIds(default!));
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DeclaredThroughResolveBatchWith()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("nonNullProducts")
                    .ResolveBatchWith<ResolveBatchWithProducts>(r => r.GetNonNullProducts(default!));
                d.Field("nullableProducts")
                    .ResolveBatchWith<ResolveBatchWithProducts>(r => r.GetNullableProducts(default!));
                d.Field("forcedNonNullProducts")
                    .ResolveBatchWith<ResolveBatchWithProducts>(r => r.GetForcedNonNullProducts(default!));
                d.Field("ids")
                    .ResolveBatchWith<ResolveBatchWithProducts>(r => r.GetIds(default!));
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DeclaredThroughInterfaceResolveBatchWith()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("items")
                    .Type<ListType<InterfaceType<IInterfaceBatchQuery>>>()
                    .Resolve(new List<IInterfaceBatchQuery>());
            })
            .AddInterfaceType<IInterfaceBatchQuery>(d =>
            {
                d.Name("InterfaceBatchQuery");
                d.Field("nonNullProducts")
                    .ResolveBatchWith<InterfaceResolveBatchWithProducts>(r => r.GetNonNullProducts(default!));
                d.Field("nullableProducts")
                    .ResolveBatchWith<InterfaceResolveBatchWithProducts>(r => r.GetNullableProducts(default!));
                d.Field("forcedNonNullProducts")
                    .ResolveBatchWith<InterfaceResolveBatchWithProducts>(r => r.GetForcedNonNullProducts(default!));
                d.Field("ids")
                    .ResolveBatchWith<InterfaceResolveBatchWithProducts>(r => r.GetIds(default!));
            })
            .AddObjectType<ObjectImplementingInterfaceBatchQuery>(
                d => d.Implements<InterfaceType<IInterfaceBatchQuery>>())
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_KeepNonNullType_From_GraphQLTypeAttribute_When_Attribute()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<GraphQLTypeAttributeBatchQuery>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_KeepNonNullType_From_GraphQLTypeAttribute_When_ResolveBatchWith()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("labels")
                    .ResolveBatchWith<ResolveBatchWithLabels>(r => r.GetLabels(default!));
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public void GetBatchReturnTypeRef_Should_Throw_NotSupportedException_When_TypeInspector_Does_Not_Override_It()
    {
        // arrange
        ITypeInspector inspector = new ForeignTypeInspector();
        var method = typeof(AttributeBatchQuery).GetMethod(nameof(AttributeBatchQuery.GetNonNullProducts))!;

        // act
        var exception = Assert.Throws<NotSupportedException>(() => inspector.GetBatchReturnTypeRef(method));

        // assert
        Assert.Contains(nameof(ForeignTypeInspector), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ITypeInspector.GetBatchReturnTypeRef), exception.Message, StringComparison.Ordinal);
    }

    public record BatchProduct(int Id, string Name);

    public class AttributeBatchQuery
    {
        [BatchResolver]
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }

    public class FieldSelectorBatchQuery
    {
        [BatchResolver]
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }

    public sealed class ResolveBatchWithProducts
    {
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }

    public interface IInterfaceBatchQuery;

    public sealed class ObjectImplementingInterfaceBatchQuery : IInterfaceBatchQuery;

    public sealed class InterfaceResolveBatchWithProducts
    {
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }

    public class GraphQLTypeAttributeBatchQuery
    {
        [BatchResolver]
        [GraphQLType(typeof(NonNullType<StringType>))]
        public List<string?> GetLabels(List<int> id)
            => id.ConvertAll(i => (string?)$"Label {i}");
    }

    public sealed class ResolveBatchWithLabels
    {
        [GraphQLType(typeof(NonNullType<StringType>))]
        public List<string?> GetLabels(List<int> id)
            => id.ConvertAll(i => (string?)$"Label {i}");
    }

    /// <summary>
    /// A minimal <see cref="ITypeInspector"/> that does not override
    /// <see cref="ITypeInspector.GetBatchReturnTypeRef"/>, exercising the default
    /// interface member for source-compatible foreign implementations.
    /// </summary>
    public sealed class ForeignTypeInspector : ITypeInspector
    {
        private readonly DefaultTypeInspector _inner = new();

        public string? Scope => _inner.Scope;

        public ReadOnlySpan<MemberInfo> GetMembers(
            Type type,
            bool includeIgnored = false,
            bool includeStatic = false,
            bool allowObject = false)
            => _inner.GetMembers(type, includeIgnored, includeStatic, allowObject);

        public ParameterInfo[] GetParameters(MethodInfo method)
            => _inner.GetParameters(method);

        public bool IsMemberIgnored(MemberInfo member)
            => _inner.IsMemberIgnored(member);

        public TypeReference GetReturnTypeRef(
            MemberInfo member,
            TypeContext context = TypeContext.None,
            string? scope = null,
            bool ignoreAttributes = false)
            => _inner.GetReturnTypeRef(member, context, scope, ignoreAttributes);

        public IExtendedType GetReturnType(MemberInfo member, bool ignoreAttributes = false)
            => _inner.GetReturnType(member, ignoreAttributes);

        public TypeReference GetArgumentTypeRef(
            ParameterInfo parameter,
            string? scope = null,
            bool ignoreAttributes = false)
            => _inner.GetArgumentTypeRef(parameter, scope, ignoreAttributes);

        public IExtendedType GetArgumentType(ParameterInfo parameter, bool ignoreAttributes = false)
            => _inner.GetArgumentType(parameter, ignoreAttributes);

        public ExtendedTypeReference GetTypeRef(
            Type type,
            TypeContext context = TypeContext.None,
            string? scope = null)
            => _inner.GetTypeRef(type, context, scope);

        public IExtendedType GetType(Type type)
            => _inner.GetType(type);

        public IExtendedType GetType(Type type, params bool?[] nullable)
            => _inner.GetType(type, nullable);

        public IExtendedType GetType(Type type, ReadOnlySpan<bool?> nullable)
            => _inner.GetType(type, nullable);

        public IEnumerable<object> GetEnumValues(Type enumType)
            => _inner.GetEnumValues(enumType);

        public MemberInfo? GetEnumValueMember(object value)
            => _inner.GetEnumValueMember(value);

        public ImmutableArray<object> GetAttributes(ICustomAttributeProvider attributeProvider, bool inherit)
            => _inner.GetAttributes(attributeProvider, inherit);

        public MemberInfo? GetNodeIdMember(Type type)
            => _inner.GetNodeIdMember(type);

        public MethodInfo? GetNodeResolverMethod(Type nodeType, Type? resolverType = null)
            => _inner.GetNodeResolverMethod(nodeType, resolverType);

        public Type ExtractNamedType(Type type)
            => _inner.ExtractNamedType(type);

        public bool IsSchemaType(Type type)
            => _inner.IsSchemaType(type);

        public bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
            => _inner.TryGetDefaultValue(parameter, out defaultValue);

        public bool TryGetDefaultValue(PropertyInfo property, out object? defaultValue)
            => _inner.TryGetDefaultValue(property, out defaultValue);

        public IExtendedType ChangeNullability(IExtendedType type, params bool?[] nullable)
            => _inner.ChangeNullability(type, nullable);

        public IExtendedType ChangeNullability(IExtendedType type, ReadOnlySpan<bool?> nullable)
            => _inner.ChangeNullability(type, nullable);

        public bool?[] CollectNullability(IExtendedType type)
            => _inner.CollectNullability(type);

        public bool CollectNullability(IExtendedType type, Span<bool?> buffer, out int written)
            => _inner.CollectNullability(type, buffer, out written);

        public ITypeInfo CreateTypeInfo(Type type)
            => _inner.CreateTypeInfo(type);

        public ITypeInfo CreateTypeInfo(IExtendedType type)
            => _inner.CreateTypeInfo(type);

        public ITypeFactory CreateTypeFactory(IExtendedType type)
            => _inner.CreateTypeFactory(type);

        public bool TryCreateTypeInfo(Type type, [NotNullWhen(true)] out ITypeInfo? typeInfo)
            => _inner.TryCreateTypeInfo(type, out typeInfo);

        public bool TryCreateTypeInfo(IExtendedType type, [NotNullWhen(true)] out ITypeInfo? typeInfo)
            => _inner.TryCreateTypeInfo(type, out typeInfo);
    }
}
