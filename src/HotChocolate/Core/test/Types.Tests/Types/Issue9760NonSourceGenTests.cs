using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

/// <summary>
/// Captures the behavior of the non-source-generated path using
/// <see cref="ObjectTypeExtension{T}"/>. These tests are the alignment
/// specification for the source-generated path.
/// </summary>
public class Issue9760NonSourceGenTests
{
    [Fact]
    public async Task DupPrefix_OnRuntimeType_Of_QueryType_Propagates()
    {
        // The non-source-gen analogue of [QueryType] [DupPrefix("dup")] partial class Query
        // is putting the attribute directly on the runtime Query type. Here the canonical
        // ObjectType<TaggedQuery> descriptor processes the attribute via
        // DescriptorAttributeHelper because FieldBindingType == typeof(TaggedQuery) carries
        // it. The OnBeforeNaming task fires on the canonical configuration during its own
        // CompleteName, so the rename propagates.
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<TaggedQuery>()
            .BuildSchemaAsync();

        var renamed = schema.Types.OfType<ObjectType>().FirstOrDefault(t => t.Name == "dup_TaggedQuery");
        Assert.NotNull(renamed);
    }

    [DupPrefix("dup")]
    public class TaggedQuery
    {
        public string FieldOne() => "one";
    }

    [Fact]
    public async Task SameAttribute_OnTwoExtensions_TargetingSameRuntimeType()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Field("entry").Resolve(new Probe { Id = "1" }))
            .AddType<Probe>()
            .AddTypeExtension<ProbeExtensionOne>()
            .AddTypeExtension<ProbeExtensionTwo>()
            .BuildSchemaAsync();

        // act
        var probeTypeNames = schema.Types
            .OfType<ObjectType>()
            .Select(t => t.Name)
            .Where(n => n.EndsWith("Probe"))
            .ToArray();

        // assert
        // Non-gen baseline: each ObjectTypeExtension<T> has its own descriptor and its own
        // [DupPrefix] attribute. The OnBeforeNaming hook fires during the extension's
        // CompleteName, mutating only the extension's configuration name. By the time the
        // type initializer merges the extension into Probe, the main type's CompleteName
        // has already run, so the prefix never reaches the canonical type. This is the
        // alignment target for the source-generated path.
        Assert.Equal(["Probe"], probeTypeNames);
    }

    [Fact]
    public async Task ConfigureOverride_OnTwoExtensions_BothApply()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Field("entry").Resolve(new Probe { Id = "1" }))
            .AddType<Probe>()
            .AddTypeExtension<ProbeExtensionOne>()
            .AddTypeExtension<ProbeExtensionTwo>()
            .BuildSchemaAsync();

        // act
        var probeType = schema.Types
            .OfType<ObjectType>()
            .Single(t => t.Name.EndsWith("Probe"));
        var fieldNames = probeType.Fields
            .Where(f => !f.IsIntrospectionField)
            .Select(f => f.Name)
            .OrderBy(n => n)
            .ToArray();

        // assert
        Assert.Equal(["configuredOne", "configuredTwo", "id"], fieldNames);
    }

    [Fact]
    public async Task SameAttribute_DifferentValues_OnTwoExtensions()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Field("entry").Resolve(new MultiValueProbe { Id = "1" }))
            .AddType<MultiValueProbe>()
            .AddTypeExtension<MultiValueProbeExtensionFirst>()
            .AddTypeExtension<MultiValueProbeExtensionSecond>()
            .BuildSchemaAsync();

        // act
        var probeTypeNames = schema.Types
            .OfType<ObjectType>()
            .Select(t => t.Name)
            .Where(n => n.EndsWith("MultiValueProbe"))
            .ToArray();

        // assert
        // Same reasoning as the previous test: each extension's prefix only mutates its
        // own configuration and never reaches MultiValueProbe.
        Assert.Equal(["MultiValueProbe"], probeTypeNames);
    }

    public class Probe
    {
        public required string Id { get; set; }
    }

    [DupPrefix("dup")]
    public class ProbeExtensionOne : ObjectTypeExtension<Probe>
    {
        protected override void Configure(IObjectTypeDescriptor<Probe> descriptor)
        {
            descriptor.Field("configuredOne").Type<NonNullType<StringType>>().Resolve("from-one");
        }
    }

    [DupPrefix("dup")]
    public class ProbeExtensionTwo : ObjectTypeExtension<Probe>
    {
        protected override void Configure(IObjectTypeDescriptor<Probe> descriptor)
        {
            descriptor.Field("configuredTwo").Type<NonNullType<StringType>>().Resolve("from-two");
        }
    }

    public class MultiValueProbe
    {
        public required string Id { get; set; }
    }

    [DupPrefix("first")]
    public class MultiValueProbeExtensionFirst : ObjectTypeExtension<MultiValueProbe>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiValueProbe> descriptor)
        {
            descriptor.Field("fieldFirst").Type<NonNullType<StringType>>().Resolve("first");
        }
    }

    [DupPrefix("second")]
    public class MultiValueProbeExtensionSecond : ObjectTypeExtension<MultiValueProbe>
    {
        protected override void Configure(IObjectTypeDescriptor<MultiValueProbe> descriptor)
        {
            descriptor.Field("fieldSecond").Type<NonNullType<StringType>>().Resolve("second");
        }
    }

    public sealed class DupPrefixAttribute(string prefix) : ObjectTypeDescriptorAttribute
    {
        public string Prefix { get; } = prefix;

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type? type)
        {
            if (type is null)
            {
                return;
            }

            var capturedPrefix = Prefix;
            descriptor
                .Extend()
                .OnBeforeNaming((_, cfg) => cfg.Name = $"{capturedPrefix}_{cfg.Name}");
        }
    }
}
