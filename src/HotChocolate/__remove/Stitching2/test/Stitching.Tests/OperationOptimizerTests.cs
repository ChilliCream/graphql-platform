using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Processing
{
    public class OperationOptimizerTests
    {
        [Fact]
        public async Task Foo()
        {
            // arrange
            var schemaFile = FileResource.Open("insurance_schema.graphql");
            var queryFile = FileResource.Open("me_query.graphql");

            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(schemaFile)
                    .UseField(next => context => default)
                    .BuildSchemaAsync();

            DocumentNode document = Utf8GraphQLParser.Parse(queryFile);

            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "abc",
                    document,
                    document.Definitions
                        .OfType<OperationDefinitionNode>()
                        .Single(),
                    schema,
                    schema.QueryType);

        }


        public class FooBar : TypeInterceptor
        {
            public override void OnAfterInitialize(
                ITypeDiscoveryContext discoveryContext,
                DefinitionBase definition,
                IDictionary<string, object> contextData)
            {
                if (definition is ObjectTypeDefinition objectTypeDefinition &&
                    objectTypeDefinition.Name.Equals("Query"))
                {
                    /*
                    var list = new List<IFetchConfiguration>();


                    objectTypeDefinition.Fields
                        .First(t => t.Name.Equals("me"))
                        .ContextData[""] = 
                    */
                }
            }
        }
    }
}
