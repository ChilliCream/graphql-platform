using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Processing
{

    public class FooTests
    {
        [Fact]
        public async Task Count_Provided_Fields_Of_A_Query()
        {
            var schemaFile = FileResource.Open("insurance_schema.graphql");
            var queryFile = FileResource.Open("me_query.graphql");

            ISchema schema = await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(schemaFile)
                .UseField(next => context => default)
                .BuildSchemaAsync();

            DocumentNode query = Utf8GraphQLParser.Parse(queryFile);

            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "abc",
                    query,
                    query.Definitions.OfType<OperationDefinitionNode>().First(),
                    schema,
                    schema.QueryType);

            SelectionSetNode provided = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                @"{
                    id
                    name
                    consultant {
                        consultantId @field(type: ""Consultant"" field: ""id"")
                        name
                    }
                }");

            ObjectType customer = schema.GetType<ObjectType>("Customer");
            ISelection root = operation.GetRootSelectionSet().Selections.First();
            ISelectionSet selectionSet = operation.GetSelectionSet(root.SelectionSet!, customer);

            var context = new MatchSelectionsContext(
                schema,
                operation,
                selectionSet.Selections.ToDictionary(t => t.Field.Name.Value),
                ImmutableStack<IOutputType>.Empty.Push(customer));

            var visitor = new MatchSelectionsVisitor();
            visitor.Visit(provided, context);

            Assert.Equal(4, context.Count);
        }

        [Fact]
        public async Task Count_Provided_Fields_Of_A_Query_With_Fragment()
        {
            var schemaFile = FileResource.Open("insurance_schema.graphql");
            var queryFile = FileResource.Open("me_query_with_fragment.graphql");

            ISchema schema = await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(schemaFile)
                .UseField(next => context => default)
                .BuildSchemaAsync();

            DocumentNode query = Utf8GraphQLParser.Parse(queryFile);

            IPreparedOperation operation =
                OperationCompiler.Compile(
                    "abc",
                    query,
                    query.Definitions.OfType<OperationDefinitionNode>().First(),
                    schema,
                    schema.QueryType);

            SelectionSetNode provided = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                @"{
                    id
                    name
                    consultant {
                        consultantId @field(type: ""Consultant"" field: ""id"")
                        name
                    }
                }");

            ObjectType customer = schema.GetType<ObjectType>("Customer");
            ISelection root = operation.GetRootSelectionSet().Selections.First();
            ISelectionSet selectionSet = operation.GetSelectionSet(root.SelectionSet!, customer);

            var context = new MatchSelectionsContext(
                schema,
                operation,
                selectionSet,
                ImmutableStack<IOutputType>.Empty.Push(customer));

            var visitor = new MatchSelectionsVisitor();
            visitor.Visit(provided, context);

            Assert.Equal(4, context.Count);
        }
    }
}
