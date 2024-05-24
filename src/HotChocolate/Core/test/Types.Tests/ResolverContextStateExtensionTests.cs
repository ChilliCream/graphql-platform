using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;

#nullable enable

namespace HotChocolate;

public class ResolverContextStateExtensionTests
{
    [Fact]
    public async Task GetUserClaims()
    {
        Snapshot.FullName();

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "abc"),
            }));

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("foo").Resolve(ctx => ctx.GetUser()?.Identity?.Name);
            })
            .ExecuteRequestAsync(
                OperationRequestBuilder.Create()
                    .SetDocument("{ foo }")
                    .SetUser(user)
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public void GetGlobalStateOrDefault_KeyMissing()
    {
        var dict = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetGlobalStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetGlobalState_KeyMissing()
    {
        var dict = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        Assert.Throws<ArgumentException>(() =>
            context.GetGlobalState<int>("key"));
    }

    [Fact]
    public void GetGlobalStateOrDefault_KeyExists_WrongType()
    {
        var dict = new Dictionary<string, object?> { { "key", "value" }, };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetGlobalStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetGlobalState_KeyExists_WrongType()
    {
        var dict = new Dictionary<string, object?> { { "key", "value" }, };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        Assert.Throws<ArgumentException>(() =>
            context.GetGlobalState<int>("key"));
    }

    [Fact]
    public void GetGlobalStateOrDefault_KeyExists_CorrectType()
    {
        var dict = new Dictionary<string, object?> { { "key", 1 }, };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetGlobalStateOrDefault<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void GetGlobalState_KeyExists_CorrectType()
    {
        var dict = new Dictionary<string, object?> { { "key", 1 }, };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetGlobalState<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void SetGlobalState()
    {
        var dict = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        context.SetGlobalState("key", "value");

        context.ContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetGlobalState_KeyMissing()
    {
        var dict = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetOrSetGlobalState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.ContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetGlobalState_KeyExists_WrongType()
    {
        var dict = new Dictionary<string, object?>
        {
            {"key", "value"},
        };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetOrSetGlobalState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.ContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetGlobalState_KeyExists_CorrectType()
    {
        var dict = new Dictionary<string, object?>
        {
            {"key", 2},
        };

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(dict);

        var context = mock.Object;

        var state = context.GetOrSetGlobalState<int>("key", key => 1);

        Assert.Equal(2, state);
        context.ContextData.MatchSnapshot();
    }

    [Fact]
    public void GetScopedStateOrDefault_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        var state = context.GetScopedStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetScopedState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        Assert.Throws<ArgumentException>(() =>
            context.GetScopedState<int>("key"));
    }

    [Fact]
    public void GetScopedStateOrDefault_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            { "key", "value" },
        }.ToImmutableDictionary();

        var state = context.GetScopedStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetScopedState_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            { "key", "value" },
        }.ToImmutableDictionary();

        Assert.Throws<ArgumentException>(() =>
            context.GetScopedState<int>("key"));
    }

    [Fact]
    public void GetScopedStateOrDefault_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            { "key", 1 },
        }.ToImmutableDictionary();

        var state = context.GetScopedStateOrDefault<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void GetScopedState_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            { "key", 1 },
        }.ToImmutableDictionary();

        var state = context.GetScopedState<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void SetScopedState()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        context.SetScopedState("key", "value");

        context.ScopedContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetScopedState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        var state = context.GetOrSetScopedState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.ScopedContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetScopedState_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            {"key", "value"},
        }.ToImmutableDictionary();

        var state = context.GetOrSetScopedState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.ScopedContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetScopedState_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = new Dictionary<string, object?>
        {
            {"key", 2},
        }.ToImmutableDictionary();

        var state = context.GetOrSetScopedState<int>("key", key => 1);

        Assert.Equal(2, state);
        context.ScopedContextData.MatchSnapshot();
    }

    [Fact]
    public void RemoveScopedState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        context.RemoveScopedState("key");

        context.ScopedContextData.MatchSnapshot();
    }

    [Fact]
    public void RemoveScopedState_KeyExists()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.ScopedContextData);

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;

        context.SetScopedState("key1", 1);
        context.SetScopedState("key2", 2);
        context.SetScopedState("key3", 3);

        context.RemoveScopedState("key2");

        Assert.False(context.ScopedContextData.ContainsKey("key2"));
    }

    [Fact]
    public void GetLocalStateOrDefault_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        var state = context.GetLocalStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetLocalState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        Assert.Throws<ArgumentException>(() =>
            context.GetLocalState<int>("key"));
    }

    [Fact]
    public void GetLocalStateOrDefault_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", "value" },
        }.ToImmutableDictionary();

        var state = context.GetLocalStateOrDefault<int>("key");

        Assert.Equal(default, state);
    }

    [Fact]
    public void GetLocalState_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", "value" },
        }.ToImmutableDictionary();

        Assert.Throws<ArgumentException>(() =>
            context.GetLocalState<int>("key"));
    }

    [Fact]
    public void GetLocalStateOrDefault_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", 1 },
        }.ToImmutableDictionary();

        var state = context.GetLocalStateOrDefault<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void GetLocalState_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", 1 },
        }.ToImmutableDictionary();

        var state = context.GetLocalState<int>("key");

        Assert.Equal(1, state);
    }

    [Fact]
    public void SetLocalState()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        context.SetLocalState("key", "value");

        context.LocalContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetLocalState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        var state = context.GetOrSetLocalState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.LocalContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetLocalState_KeyExists_WrongType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", "value" },
        }.ToImmutableDictionary();

        var state = context.GetOrSetLocalState<int>("key", key => 1);

        Assert.Equal(1, state);
        context.LocalContextData.MatchSnapshot();
    }

    [Fact]
    public void GetOrSetLocalState_KeyExists_CorrectType()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = new Dictionary<string, object?>
        {
            { "key", 2 },
        }.ToImmutableDictionary();

        var state = context.GetOrSetLocalState<int>("key", key => 1);

        Assert.Equal(2, state);
        context.LocalContextData.MatchSnapshot();
    }

    [Fact]
    public void RemoveLocalState_KeyMissing()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        context.RemoveLocalState("key");

        context.LocalContextData.MatchSnapshot();
    }

    [Fact]
    public void RemoveLocalState_KeyExists()
    {
        var mock = new Mock<IResolverContext>();
        mock.SetupProperty(c => c.LocalContextData);

        var context = mock.Object;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;

        context.SetLocalState("key1", 1);
        context.SetLocalState("key2", 2);
        context.SetLocalState("key3", 3);

        context.RemoveLocalState("key2");

        Assert.False(context.LocalContextData.ContainsKey("key2"));
    }
}
