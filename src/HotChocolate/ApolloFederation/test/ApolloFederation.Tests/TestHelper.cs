using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using HotChocolate.Resolvers;
using Moq;

namespace HotChocolate.ApolloFederation;

public static class TestHelper
{
    public static IResolverContext CreateResolverContext(ISchema schema)
    {
        var contextData = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>();
        mock.SetupGet(c => c.ContextData).Returns(contextData);
        mock.SetupProperty(c => c.ScopedContextData);
        mock.SetupProperty(c => c.LocalContextData);
        mock.SetupGet(c => c.Schema).Returns(schema);

        IResolverContext context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;
        return context;
    }
}
