using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Stitching
{
    public class ExtractFieldQuerySyntaxRewriterTests
    {
        [InlineData("Stitching.graphql", "StitchingQuery.graphql")]
        [InlineData("Stitching.graphql", "StitchingQueryWithFragmentDefs.graphql")]
        [InlineData("Stitching.graphql", "StitchingQueryWithInlineFragment.graphql")]
        [InlineData("Stitching.graphql", "StitchingQueryWithUnion.graphql")]
        [InlineData("Stitching.graphql", "StitchingQueryWithVariables.graphql")]
        [InlineData("Stitching.graphql", "StitchingQueryWithArguments.graphql")]
        [InlineData("StitchingComputed.graphql", "StitchingQueryComputedField.graphql")]
        [Theory]
        public void ExtractField(string schemaFile, string queryFile)
        {
            // arrange
            ISchema schema = Schema.Create(
                FileResource.Open(schemaFile),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<SchemaDirectiveType>();
                    c.RegisterDirective<ComputedDirectiveType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            DocumentNode query = Parser.Default.Parse(
                FileResource.Open(queryFile));

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().Single();

            FieldNode selection = operation
                .SelectionSet.Selections
                .OfType<FieldNode>().First();

            // act
            var rewriter = new ExtractFieldQuerySyntaxRewriter(schema);
            ExtractedField extractedField = rewriter.ExtractField(
                query, operation, selection,
                schema.GetType<ObjectType>("Query"));

            // assert
            DocumentNode document = RemoteQueryBuilder.New()
                .SetRequestField(extractedField.Field)
                .AddFragmentDefinitions(extractedField.Fragments)
                .AddVariables(extractedField.Variables)
                .Build();

            QuerySyntaxSerializer.Serialize(document)
                .Snapshot(nameof(ExtractField) + "_" + queryFile);
        }
    }
}
