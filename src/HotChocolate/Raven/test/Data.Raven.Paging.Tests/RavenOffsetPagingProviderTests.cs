using HotChocolate.Data.Raven.Pagination;
using HotChocolate.Types.Descriptors;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data;

public class RavenOffsetPagingProviderTests
{
    [Theory]
    [InlineData(nameof(RavenQueryable), true)]
    [InlineData(nameof(AsyncDocumentQuery), true)]
    public void CanHandle_MethodReturnType_MatchesResult(string methodName, bool expected)
    {
        // arrange
        var provider = new RavenOffsetPagingProvider();
        var member = typeof(RavenOffsetPagingProviderTests).GetMethod(methodName)!;
        var type = new DefaultTypeInspector().GetReturnType(member);

        // act
        var result = provider.CanHandle(type);

        // assert
        Assert.Equal(expected, result);
    }

    public IRavenQueryable<Foo> RavenQueryable() => throw new InvalidOperationException();

    public IAsyncDocumentQuery<Foo> AsyncDocumentQuery() => throw new InvalidOperationException();

    public class Foo
    {
    }
}
