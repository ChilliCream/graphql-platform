using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class AbstractParentBatchTests
{
    public static readonly IReadOnlyList<ICharacter> Characters =
    [
        new AbstractHuman(1, "Luke"),
        new AbstractDroid(2, "R2D2")
    ];

    public static readonly IReadOnlyList<object> Vehicles =
    [
        new AbstractCar(1, "Model S"),
        new AbstractBike(2, "BMX")
    ];

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("characters").Type<ListType<InterfaceType<ICharacter>>>().Resolve(Characters);
                d.Field("vehicles").Type<ListType<AbstractVehicleUnionType>>().Resolve(Vehicles);
            })
            .AddInterfaceType<ICharacter>(d => d.Field(c => c.Name))
            .AddObjectType<AbstractHuman>(d => d.Implements<InterfaceType<ICharacter>>())
            .AddObjectType<AbstractDroid>(d => d.Implements<InterfaceType<ICharacter>>())
            .AddTypeExtension<AbstractHumanAttributeExtension>()
            .AddTypeExtension<AbstractDroidAttributeExtension>()
            .AddTypeExtension<AbstractCarAttributeExtension>()
            .AddTypeExtension<AbstractBikeAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("characters").Type<ListType<InterfaceType<ICharacter>>>().Resolve(Characters);
                d.Field("vehicles").Type<ListType<AbstractVehicleUnionType>>().Resolve(Vehicles);
            })
            .AddInterfaceType<ICharacter>(AbstractCharacterInterface.Initialize)
            .AddObjectType<AbstractHuman>(AbstractHumanNode.Initialize)
            .AddObjectType<AbstractDroid>(AbstractDroidNode.Initialize)
            .AddObjectType<AbstractCar>(AbstractCarNode.Initialize)
            .AddObjectType<AbstractBike>(AbstractBikeNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("characters").Type<ListType<InterfaceType<ICharacter>>>().Resolve(Characters);
                d.Field("vehicles").Type<ListType<AbstractVehicleUnionType>>().Resolve(Vehicles);
            })
            .AddInterfaceType<ICharacter>(d => d.Field(c => c.Name))
            .AddObjectType<AbstractHuman>(d =>
            {
                d.Implements<InterfaceType<ICharacter>>();
                d.Field(h => h.Name);
                d.Field("friends")
                    .ResolveBatchWith<FluentAbstractHumanResolvers>(t => t.GetFriends(default!, default!));
            })
            .AddObjectType<AbstractDroid>(d =>
            {
                d.Implements<InterfaceType<ICharacter>>();
                d.Field(dr => dr.Name);
                d.Field("friends")
                    .ResolveBatchWith<FluentAbstractDroidResolvers>(t => t.GetFriends(default!, default!));
            })
            .AddObjectType<AbstractCar>(d =>
            {
                d.Field(c => c.Model);
                d.Field("spec")
                    .ResolveBatchWith<FluentAbstractCarResolvers>(t => t.GetSpec(default!, default!));
            })
            .AddObjectType<AbstractBike>(d =>
            {
                d.Field(b => b.Model);
                d.Field("spec")
                    .ResolveBatchWith<FluentAbstractBikeResolvers>(t => t.GetSpec(default!, default!));
            });
}

/// <summary>
/// The abstract parent every declaration style dispatches the same-named <c>friends</c> field
/// against, once per concrete implementing type (hc-0-6cq.8).
/// </summary>
public interface ICharacter
{
    string Name { get; }
}

public sealed record AbstractHuman(int Id, string Name) : ICharacter;

public sealed record AbstractDroid(int Id, string Name) : ICharacter;

public sealed record AbstractCar(int Id, string Model);

public sealed record AbstractBike(int Id, string Model);

/// <summary>
/// The union every declaration style dispatches the same-named <c>spec</c> field against, once
/// per member type, mirroring the interface proof for a parent with no shared base type.
/// </summary>
public sealed class AbstractVehicleUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("AbstractVehicle");
        descriptor.Type<ObjectType<AbstractCar>>();
        descriptor.Type<ObjectType<AbstractBike>>();
    }
}

// -- Fluent -----------------------------------------------------------------------------------

public sealed class FluentAbstractHumanResolvers
{
    public List<string> GetFriends([Parent] List<AbstractHuman> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"human-friend-of-{p.Name}");
    }
}

public sealed class FluentAbstractDroidResolvers
{
    public List<string> GetFriends([Parent] List<AbstractDroid> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"droid-friend-of-{p.Name}");
    }
}

public sealed class FluentAbstractCarResolvers
{
    public List<string> GetSpec([Parent] List<AbstractCar> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"car-spec-of-{p.Model}");
    }
}

public sealed class FluentAbstractBikeResolvers
{
    public List<string> GetSpec([Parent] List<AbstractBike> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"bike-spec-of-{p.Model}");
    }
}

// -- Attribute ----------------------------------------------------------------------------------

[ExtendObjectType<AbstractHuman>]
public sealed class AbstractHumanAttributeExtension
{
    [BatchResolver]
    public List<string> GetFriends([Parent] List<AbstractHuman> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"human-friend-of-{p.Name}");
    }
}

[ExtendObjectType<AbstractDroid>]
public sealed class AbstractDroidAttributeExtension
{
    [BatchResolver]
    public List<string> GetFriends([Parent] List<AbstractDroid> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"droid-friend-of-{p.Name}");
    }
}

[ExtendObjectType<AbstractCar>]
public sealed class AbstractCarAttributeExtension
{
    [BatchResolver]
    public List<string> GetSpec([Parent] List<AbstractCar> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"car-spec-of-{p.Model}");
    }
}

[ExtendObjectType<AbstractBike>]
public sealed class AbstractBikeAttributeExtension
{
    [BatchResolver]
    public List<string> GetSpec([Parent] List<AbstractBike> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"bike-spec-of-{p.Model}");
    }
}

// -- Source generated -----------------------------------------------------------------------

[InterfaceType<ICharacter>]
public static partial class AbstractCharacterInterface
{
    public static string GetName([Parent] ICharacter character) => character.Name;
}

[ObjectType<AbstractHuman>]
public static partial class AbstractHumanNode
{
    [BatchResolver]
    public static List<string> GetFriends([Parent] List<AbstractHuman> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"human-friend-of-{p.Name}");
    }
}

[ObjectType<AbstractDroid>]
public static partial class AbstractDroidNode
{
    [BatchResolver]
    public static List<string> GetFriends([Parent] List<AbstractDroid> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetFriends), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"droid-friend-of-{p.Name}");
    }
}

[ObjectType<AbstractCar>]
public static partial class AbstractCarNode
{
    [BatchResolver]
    public static List<string> GetSpec([Parent] List<AbstractCar> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"car-spec-of-{p.Model}");
    }
}

[ObjectType<AbstractBike>]
public static partial class AbstractBikeNode
{
    [BatchResolver]
    public static List<string> GetSpec([Parent] List<AbstractBike> parents, BatchProbe probe)
    {
        probe.Record(nameof(GetSpec), parents.Select(p => p.Id));
        return parents.ConvertAll(p => $"bike-spec-of-{p.Model}");
    }
}
