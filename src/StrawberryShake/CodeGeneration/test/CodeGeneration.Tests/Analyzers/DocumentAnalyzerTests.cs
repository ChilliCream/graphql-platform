using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class DocumentAnalyzerTests
    {
        [Fact]
        public void Foo()
        {
            ISchema schema =
                SchemaBuilder.New()
                    .Use(next => context => Task.CompletedTask)
                    .AddDocumentFromString(@"
                    type Query {
                      foo: Foo
                    }

                    interface Foo {
                      id: String
                      name: String
                    }

                    type Bar implements Foo {
                      id: String
                      name: String
                      bar: String
                    }

                    type Baz implements Foo {
                      id: String
                      name: String
                      baz: String
                    }")
                    .Create();

            DocumentNode document =
                Utf8GraphQLParser.Parse(@"
                query getFoo {
                  foo {
                    id
                    name
                    ... on Bar {
                      bar
                    }
                    ... on Baz {
                      baz
                    }
                  }
                }");

            var analyzer = new DocumentAnalyzer();
            analyzer.SetSchema(schema);
            analyzer.AddDocument(document);
            analyzer.SetHashProvider(new MD5DocumentHashProvider(HashFormat.Hex));

            analyzer.Analyze().ToJson().MatchSnapshot();
        }
    }
}
