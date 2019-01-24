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
        [InlineData("StitchingQuery.graphql")]
        [InlineData("StitchingQueryWithFragmentDefs.graphql")]
        [InlineData("StitchingQueryWithInlineFragment.graphql")]
        [InlineData("StitchingQueryWithUnion.graphql")]
        [InlineData("StitchingQueryWithVariables.graphql")]
        [InlineData("StitchingQueryWithArguments.graphql")]
        [Theory]
        public void ExtractField(string queryFile)
        {
            // arrange
            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<SchemaDirectiveType>();
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
