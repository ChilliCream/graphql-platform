using System.Collections.Immutable;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Moq;

namespace HotChocolate.ApolloFederation;

public static class TestHelper
{
    public static IResolverContext CreateResolverContext(
        ISchema schema,
        ObjectType? type = null,
        Action<Mock<IResolverContext>>? additionalMockSetup = null)
    {
        var contextData = new Dictionary<string, object?>();

        var mock = new Mock<IResolverContext>(MockBehavior.Strict);
        mock.SetupGet(c => c.RequestAborted).Returns(CancellationToken.None);
        mock.SetupGet(c => c.ContextData).Returns(contextData);
        mock.SetupProperty(c => c.ScopedContextData);
        mock.SetupProperty(c => c.LocalContextData);
        mock.Setup(c => c.Parent<_Service>()).Returns(new _Service());
        mock.Setup(c => c.Clone()).Returns(mock.Object);
        mock.SetupGet(c => c.Schema).Returns(schema);

        if (type is not null)
        {
            mock.SetupGet(c => c.ObjectType).Returns(type);
        }

        if (additionalMockSetup is not null)
        {
            additionalMockSetup(mock);
        }

        var context = mock.Object;
        context.ScopedContextData = ImmutableDictionary<string, object?>.Empty;
        context.LocalContextData = ImmutableDictionary<string, object?>.Empty;
        return context;
    }

    public static Representation RepresentationOf<T>(string typeName, T anonymousObject)
        where T : class
    {
        var fields = anonymousObject
            .GetType()
            .GetProperties()
            .Select(p =>
            {
                var value = p.GetValue(anonymousObject);
                var result = value switch
                {
                    null => new ObjectFieldNode(p.Name, NullValueNode.Default),
                    string s => new ObjectFieldNode(p.Name, s),
                    int i => new ObjectFieldNode(p.Name, i),
                    bool b => new ObjectFieldNode(p.Name, b),
                    _ => throw new NotSupportedException($"Type {p.PropertyType} is not supported"),
                };
                return result;
            })
            .ToArray();

        return new Representation(typeName, new ObjectValueNode(fields));
    }

    public static List<Representation> RepresentationsOf<T>(string typeName, params T[] anonymousObjects)
        where T : class
    {
        return anonymousObjects
            .Select(o => RepresentationOf(typeName, o))
            .ToList();
    }
}
