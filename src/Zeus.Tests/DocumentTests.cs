using System.Threading;
using System.Threading.Tasks;
using GraphQLParser;
using Xunit;
using Zeus.Execution;

namespace Zeus.Tests
{
    public class DocumentTests
    {
        [Fact]
        public void Foo()
        {
            // arrange
            Schema schema = Schema.Create("type Foo { c : [Bar] } type Bar { d : String } type Query { b : Foo }", new ResolverCollectionMock());
            Document document = Document.Parse("query a { b { c { d } } }");

            // act
            RequestExecuter executer = new RequestExecuter();
            executer.ExecuteAsync(schema, document, null, null, null, CancellationToken.None).Wait();

            // act
        }
    }

    public class ResolverCollectionMock
        : IResolverCollection
    {
        public bool TryGetResolver(string typeName, string fieldName, out IResolver resolver)
        {
            resolver = new Resolver();
            return true;
        }
    }

    public class Resolver
        : IResolver
    {
        static int i = 0;
        public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(i++);
        }
    }
}