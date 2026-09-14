using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class InterfaceBatchTests
{
    public static readonly IReadOnlyList<InterfaceUser> Users =
    [
        new InterfaceUser(1, "Alice"),
        new InterfaceUser(2, "Bob"),
        new InterfaceUser(3, "Charlie")
    ];

    private void RegisterServices(IRequestExecutorBuilder builder)
        => builder.Services.AddSingleton<InterfaceGreetingService>();

    /// <summary>
    /// Attribute-style reflection over the interface's own CLR members cannot express a batch
    /// field: a static [BatchResolver] method yields no field at all, and a default interface
    /// method yields a field but no batch dispatch. InterfaceFieldDescriptor.cs:55-63 does set
    /// CoreFieldFlags.BatchResolver from a [BatchResolver] MethodInfo, but the default-method
    /// resolver still runs once per selection and its single batched result is misassigned back
    /// as every parent's own value, so each context surfaces null entries and leaf-coercion
    /// errors instead of one resolved item per parent.
    /// </summary>
    private const string AttributeNotApplicableReason =
        "reflection over the interface's own members either drops the field (a static "
        + "[BatchResolver] method) or wires no batch dispatch behind it (a default interface "
        + "method runs once but its batched result is misassigned back per parent, surfacing "
        + "null entries and leaf-coercion errors); InterfaceFieldDescriptor.cs:55-63 sets "
        + "CoreFieldFlags.BatchResolver but nothing consumes it for either shape";

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
    {
        RegisterServices(builder);
        builder
            .AddQueryType(d => d.Name("Query")
                .Field("users").Type<ListType<InterfaceType<IInterfaceUser>>>().Resolve(Users))
            .AddInterfaceType<IInterfaceUser>(InterfaceUserInterface.Initialize)
            .AddObjectType<InterfaceUser>(InterfaceUserNode.Initialize);
    }

    private void ConfigureFluent(IRequestExecutorBuilder builder)
    {
        RegisterServices(builder);
        builder
            .AddQueryType(d => d.Name("Query")
                .Field("users").Type<ListType<InterfaceType<IInterfaceUser>>>().Resolve(Users))
            .AddInterfaceType<IInterfaceUser>(d =>
            {
                d.Field(u => u.Name);
                d.Field("greeting")
                    .ResolveBatchWith<FluentInterfaceUserResolvers>(t => t.GetGreeting(default!, default!));
                d.Field("greetingWithArgument")
                    .Argument("prefix", a => a.Type<StringType>())
                    .ResolveBatchWith<FluentInterfaceUserResolvers>(t => t.GetGreetingWithArgument(default!, default!));
                d.Field("greetingWithService")
                    .ResolveBatchWith<FluentInterfaceUserResolvers>(t => t.GetGreetingWithService(default!, default!));
            })
            .AddObjectType<InterfaceUser>(d => d.Implements<InterfaceType<IInterfaceUser>>());
    }
}

/// <summary>
/// A user resolved as an implementation of <see cref="IInterfaceUser"/>.
/// </summary>
public sealed record InterfaceUser(int Id, string Name) : IInterfaceUser;

public sealed class InterfaceGreetingService
{
    public string Greet(string name) => $"Hello, {name}!";
}

/// <summary>
/// The interface every declaration style resolves batch fields against. Its <c>greeting*</c>
/// fields are declared once at the interface level and inherited by every implementing type.
/// </summary>
public interface IInterfaceUser
{
    string Name { get; }
}

/// <summary>
/// Fluent-style batch resolvers bound with <c>ResolveBatchWith</c> on the interface field.
/// </summary>
public sealed class FluentInterfaceUserResolvers
{
    public List<string> GetGreeting([Parent] List<IInterfaceUser> users, BatchProbe probe)
    {
        probe.Record(nameof(GetGreeting), users.Select(u => u.Name));
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    public List<string> GetGreetingWithArgument([Parent] List<IInterfaceUser> users, List<string> prefix)
        => users.Zip(prefix, (u, p) => $"{p}, {u.Name}!").ToList();

    public List<string> GetGreetingWithService(
        [Parent] List<IInterfaceUser> users,
        [Service] InterfaceGreetingService service)
        => users.ConvertAll(u => service.Greet(u.Name));
}

/// <summary>
/// Source-generated interface type. Batch resolvers declared here apply to every implementing
/// object type, exactly like the fluent interface-level registration.
/// </summary>
[InterfaceType<IInterfaceUser>]
public static partial class InterfaceUserInterface
{
    [BatchResolver]
    public static List<string> GetGreeting([Parent] List<IInterfaceUser> users, BatchProbe probe)
    {
        probe.Record(nameof(GetGreeting), users.Select(u => u.Name));
        return users.ConvertAll(u => $"Hello, {u.Name}!");
    }

    [BatchResolver]
    public static List<string> GetGreetingWithArgument([Parent] List<IInterfaceUser> users, List<string> prefix)
        => users.Zip(prefix, (u, p) => $"{p}, {u.Name}!").ToList();

    [BatchResolver]
    public static List<string> GetGreetingWithService(
        [Parent] List<IInterfaceUser> users,
        [Service] InterfaceGreetingService service)
        => users.ConvertAll(u => service.Greet(u.Name));
}

[ObjectType<InterfaceUser>]
public static partial class InterfaceUserNode;
