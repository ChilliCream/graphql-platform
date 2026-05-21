using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

// Mirrors Issue9760NonSourceGenTests in the non-source-gen test project. The source
// generator emits a synthetic ObjectTypeExtension per [ObjectType<T>] partial, so
// multiple partials targeting the same runtime type behave identically to hand-written
// ObjectTypeExtension<T> instances: each carries its own descriptor, extension-scoped
// lifecycle hooks fire on the extension's own configuration, and the merge step does
// not propagate the resulting name changes to the canonical type.
public class Issue9760Tests
{
    [Fact]
    public async Task ObjectTypeDescriptorAttribute_AppliesOnce_When_Multiple_StaticPartials_Carry_SameAttribute()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        // act
        var probeTypeNames = schema.Types
            .OfType<ObjectType>()
            .Select(t => t.Name)
            .Where(n => n.EndsWith("Issue9760Probe"))
            .ToArray();

        // assert
        // Class-level attributes on [ObjectType<T>] partials are applied to the canonical
        // descriptor (mirroring how attributes on the runtime type T behave in non-gen),
        // deduplicated by attribute identity, so the same [Issue9760Prefix("dup")] on N
        // partials renames the merged type to "dup_Issue9760Probe" once.
        Assert.Equal(["dup_Issue9760Probe"], probeTypeNames);
    }

    [Fact]
    public async Task Configure_PartialMethod_Should_Run_For_Each_StaticPartial_Targeting_SameRuntimeType()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        // act
        var probeType = schema.Types.GetType<ObjectType>("dup_Issue9760Probe");
        var fieldNames = probeType.Fields
            .Where(f => !f.IsIntrospectionField)
            .Select(f => f.Name)
            .OrderBy(n => n)
            .ToArray();

        // assert
        Assert.Equal(["configuredOne", "configuredTwo", "fieldOne", "fieldTwo", "id"], fieldNames);
    }

    [Fact]
    public async Task ObjectTypeDescriptorAttribute_EachDistinctValueApplies_When_Multiple_StaticPartials_Carry_SameAttribute()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddIntegrationTestTypes()
            .AddPagingArguments()
            .BuildSchemaAsync();

        // act
        var probeTypeNames = schema.Types
            .OfType<ObjectType>()
            .Select(t => t.Name)
            .Where(n => n.EndsWith("_Issue9760MultiValueProbe"))
            .ToArray();

        // assert
        // Distinct attribute argument values are deduplicated independently, so both
        // [Issue9760Prefix("first")] and [Issue9760Prefix("second")] apply once each.
        Assert.Single(probeTypeNames);
        Assert.Contains("first_", probeTypeNames[0]);
        Assert.Contains("second_", probeTypeNames[0]);
    }
}
