using System.Reflection;
using System.Xml.Linq;

namespace HotChocolate.Types.Descriptors;

public class XmlDocumentationFileResolverTests
{
    [Fact]
    public async Task TryGetXmlDocument_Should_ReturnCachedDocument_When_CalledConcurrently()
    {
        // arrange
        using var firstLoadStarted = new ManualResetEventSlim();
        using var secondCallStarting = new ManualResetEventSlim();
        var cancellationToken = TestContext.Current.CancellationToken;
        var resolveCount = 0;
        var resolver = new XmlDocumentationFileResolver(_ =>
        {
            Interlocked.Increment(ref resolveCount);
            firstLoadStarted.Set();
            secondCallStarting.Wait(cancellationToken);
            return "HotChocolate.Types.Tests.Documentation.xml";
        });
        var assembly = typeof(BaseBaseClass).Assembly;

        // act
        var firstCall = Task.Run(() => ResolveDocument(resolver, assembly));
        firstLoadStarted.Wait(cancellationToken);

        var secondCall = Task.Run(() =>
        {
            secondCallStarting.Set();
            return ResolveDocument(resolver, assembly);
        });

        var documents = await Task.WhenAll(firstCall, secondCall);

        // assert
        Assert.All(documents, Assert.NotNull);
        Assert.Same(documents[0], documents[1]);
        Assert.Equal(1, resolveCount);
    }

    [Fact]
    public void TryGetXmlDocument_Should_CacheMissingFile_When_CalledRepeatedly()
    {
        // arrange
        var resolveCount = 0;
        var resolver = new XmlDocumentationFileResolver(_ =>
        {
            Interlocked.Increment(ref resolveCount);
            return $"missing-{Guid.NewGuid():N}.xml";
        });
        var assembly = typeof(BaseBaseClass).Assembly;

        // act
        var firstResult = resolver.TryGetXmlDocument(assembly, out var firstDocument);
        var secondResult = resolver.TryGetXmlDocument(assembly, out var secondDocument);

        // assert
        Assert.False(firstResult);
        Assert.Null(firstDocument);
        Assert.False(secondResult);
        Assert.Null(secondDocument);
        Assert.Equal(1, resolveCount);
    }

    private static XDocument? ResolveDocument(
        IXmlDocumentationFileResolver resolver,
        Assembly assembly) =>
        resolver.TryGetXmlDocument(assembly, out var document)
            ? document
            : null;
}
