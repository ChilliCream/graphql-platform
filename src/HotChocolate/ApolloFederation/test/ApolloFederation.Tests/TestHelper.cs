using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;

namespace HotChocolate.ApolloFederation;

public static class TestHelper
{
    public static IResolverContext CreateResolverContext(ISchema schema, ObjectType? type = null)
    {
        var contextData = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>(MockBehavior.Strict);
        mock.SetupGet(c => c.ContextData).Returns(contextData);
        mock.SetupProperty(c => c.ScopedContextData);
        mock.SetupProperty(c => c.LocalContextData);
        mock.SetupGet(c => c.Schema).Returns(schema);

        if (type is not null)
        {
            mock.SetupGet(c => c.ObjectType).Returns(type);
        }

        IResolverContext context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;
        return context;
    }
}
