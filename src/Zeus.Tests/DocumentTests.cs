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
            int i = 0;

            IResolverCollection resolverCollection = ResolverBuilder.Create()
                .Add("Query", "b", () => i++)
                .Add("Bar", "d", () => i++)
                .Add("Foo", "c", () => i++)
                .Build();
            ISchema schema = Schema.Create("type Foo { c : [Bar] } type Bar { d : String } type Query { b : Foo }", resolverCollection);
            Document document = Document.Parse("query a { b { c { d } } }");


            // act
            DocumentExecuter documentExecuter = new DocumentExecuter();
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