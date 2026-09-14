using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.BatchResolvers;

public sealed partial class AuthorizationBatchTests
{
    /// <summary>
    /// The fixed set of users every declaration style exposes for the type-protected
    /// batch field scenario.
    /// </summary>
    public static readonly IReadOnlyList<AuthUser> Users = [new AuthUser("a")];

    private void ConfigureAttribute(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType<AuthAttributeQuery>()
            .AddTypeExtension<AuthUserAttributeExtension>();

    private void ConfigureSourceGenerated(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(AuthQuery.Initialize)
            .AddObjectType<AuthUser>(AuthUserNode.Initialize);

    private void ConfigureFluent(IRequestExecutorBuilder builder)
        => builder
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("beforeSecretById")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Authorize("READ_SECRET_BEFORE")
                    .ResolveBatchWith<FluentAuthResolvers>(t => t.GetBeforeSecretById(null!, null!));
                d.Field("afterSecretById")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Authorize("READ_SECRET_AFTER", ApplyPolicy.AfterResolver)
                    .ResolveBatchWith<FluentAuthResolvers>(t => t.GetAfterSecretById(null!, null!));
                d.Field("thingById")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Authorize("READ_THING", ApplyPolicy.Validation)
                    .ResolveBatchWith<FluentAuthResolvers>(t => t.GetThingById(null!, null!));
                d.Field("allowedById")
                    .Argument("id", a => a.Type<NonNullType<IntType>>())
                    .Authorize("READ_ALLOWED")
                    .ResolveBatchWith<FluentAuthResolvers>(t => t.GetAllowedById(null!, null!));
                d.Field("users").Type<ListType<ObjectType<AuthUser>>>().Resolve(Users);
            })
            .AddObjectType<ProtectedFriend>(d => d.Authorize("READ_FRIEND"))
            .AddObjectType<AuthUser>(d =>
                d.Field("friend")
                    .ResolveBatchWith<FluentAuthResolvers>(t => t.GetFriend(null!, null!)));
}

public sealed record Secret(int Id, string Value);

public sealed record Thing(int Id);

public sealed record Allowed(int Id);

public sealed record AuthUser(string Key);

/// <summary>
/// The batch-resolved type returned by <see cref="AuthUser.Key"/>'s <c>friend</c> field. The
/// policy lives on the returned type, not the field, so authorization is enforced through the
/// type-injected middleware rather than a field-level directive.
/// </summary>
[Authorize("READ_FRIEND")]
public sealed record ProtectedFriend(string Id);

/// <summary>
/// Fluent-style batch resolvers for every authorization row, decorated purely through
/// <c>descriptor.Authorize(...)</c> calls in the fluent <c>Configure</c> method rather than
/// attributes.
/// </summary>
public sealed class FluentAuthResolvers
{
    public List<Secret?> GetBeforeSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetBeforeSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    public List<Secret?> GetAfterSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAfterSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    public List<Thing?> GetThingById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetThingById), id);
        return id.ConvertAll(i => (Thing?)new Thing(i));
    }

    public List<Allowed?> GetAllowedById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAllowedById), id);
        return id.ConvertAll(i => (Allowed?)new Allowed(i));
    }

    public List<ProtectedFriend?> GetFriend([Parent] List<AuthUser> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Key);
        probe.Record(nameof(GetFriend), keys);
        return keys.ConvertAll(k => (ProtectedFriend?)new ProtectedFriend(k));
    }
}

/// <summary>
/// Attribute-style root query exposing every authorization row through
/// <see cref="AuthorizeAttribute"/> on the batch resolver method.
/// </summary>
public sealed class AuthAttributeQuery
{
    [BatchResolver]
    [Authorize("READ_SECRET_BEFORE")]
    public List<Secret?> GetBeforeSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetBeforeSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    [BatchResolver]
    [Authorize("READ_SECRET_AFTER", ApplyPolicy.AfterResolver)]
    public List<Secret?> GetAfterSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAfterSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    [BatchResolver]
    [Authorize("READ_THING", ApplyPolicy.Validation)]
    public List<Thing?> GetThingById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetThingById), id);
        return id.ConvertAll(i => (Thing?)new Thing(i));
    }

    [BatchResolver]
    [Authorize("READ_ALLOWED")]
    public List<Allowed?> GetAllowedById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAllowedById), id);
        return id.ConvertAll(i => (Allowed?)new Allowed(i));
    }

    public IReadOnlyList<AuthUser> GetUsers() => AuthorizationBatchTests.Users;
}

/// <summary>
/// Attribute-style batch resolver returning the type-protected <see cref="ProtectedFriend"/>.
/// </summary>
[ExtendObjectType<AuthUser>]
public sealed class AuthUserAttributeExtension
{
    [BatchResolver]
    public List<ProtectedFriend?> GetFriend([Parent] List<AuthUser> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Key);
        probe.Record(nameof(GetFriend), keys);
        return keys.ConvertAll(k => (ProtectedFriend?)new ProtectedFriend(k));
    }
}

/// <summary>
/// Source-generated root query exposing every authorization row through
/// <see cref="AuthorizeAttribute"/> on the batch resolver method.
/// </summary>
[QueryType]
public static partial class AuthQuery
{
    [BatchResolver]
    [Authorize("READ_SECRET_BEFORE")]
    public static List<Secret?> GetBeforeSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetBeforeSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    [BatchResolver]
    [Authorize("READ_SECRET_AFTER", ApplyPolicy.AfterResolver)]
    public static List<Secret?> GetAfterSecretById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAfterSecretById), id);
        return id.ConvertAll(i => (Secret?)new Secret(i, $"secret-{i}"));
    }

    [BatchResolver]
    [Authorize("READ_THING", ApplyPolicy.Validation)]
    public static List<Thing?> GetThingById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetThingById), id);
        return id.ConvertAll(i => (Thing?)new Thing(i));
    }

    [BatchResolver]
    [Authorize("READ_ALLOWED")]
    public static List<Allowed?> GetAllowedById(List<int> id, BatchProbe probe)
    {
        probe.Record(nameof(GetAllowedById), id);
        return id.ConvertAll(i => (Allowed?)new Allowed(i));
    }

    public static IReadOnlyList<AuthUser> GetUsers() => AuthorizationBatchTests.Users;
}

/// <summary>
/// Source-generated batch resolver returning the type-protected <see cref="ProtectedFriend"/>.
/// </summary>
[ObjectType<AuthUser>]
public static partial class AuthUserNode
{
    [BatchResolver]
    public static List<ProtectedFriend?> GetFriend([Parent] List<AuthUser> parents, BatchProbe probe)
    {
        var keys = parents.ConvertAll(p => p.Key);
        probe.Record(nameof(GetFriend), keys);
        return keys.ConvertAll(k => (ProtectedFriend?)new ProtectedFriend(k));
    }
}
