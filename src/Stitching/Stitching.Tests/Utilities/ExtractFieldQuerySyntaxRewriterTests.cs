using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Types;
using Snapshooter;
using Snapshooter.Xunit;
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
        [InlineData("Stitching.graphql", "StitchingQueryWithTypename.graphql")]
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
                    c.RegisterDirective<ComputedDirectiveType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open(queryFile));

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().Single();

            FieldNode selection = operation
                .SelectionSet.Selections
                .OfType<FieldNode>().First();

            // act
            var rewriter = new ExtractFieldQuerySyntaxRewriter(schema,
                Array.Empty<IQueryDelegationRewriter>());
            ExtractedField extractedField = rewriter.ExtractField(
                "customer", query, operation, selection,
                schema.GetType<ObjectType>("Query"));

            // assert
            DocumentNode document = RemoteQueryBuilder.New()
                .SetRequestField(extractedField.Field)
                .AddFragmentDefinitions(extractedField.Fragments)
                .AddVariables(extractedField.Variables)
                .Build();

            QuerySyntaxSerializer.Serialize(document)
                .MatchSnapshot(new SnapshotNameExtension(
                    schemaFile, queryFile));
        }

        [Fact]
        public void ExtractField_WithCustomRewriters()
        {
            // arrange
            ISchema schema = Schema.Create(
                FileResource.Open("Stitching.graphql"),
                c =>
                {
                    c.RegisterType<DateTimeType>();
                    c.RegisterDirective<DelegateDirectiveType>();
                    c.RegisterDirective<ComputedDirectiveType>();
                    c.Use(next => context => Task.CompletedTask);
                });

            DocumentNode query = Utf8GraphQLParser.Parse(
                FileResource.Open("StitchingQuery.graphql"));

            OperationDefinitionNode operation = query.Definitions
                .OfType<OperationDefinitionNode>().Single();

            FieldNode selection = operation
                .SelectionSet.Selections
                .OfType<FieldNode>().First();

            var rewriters = new List<IQueryDelegationRewriter>
            {
                new DummyRewriter()
            };

            // act
            var rewriter = new ExtractFieldQuerySyntaxRewriter(
                schema, rewriters);

            ExtractedField extractedField = rewriter.ExtractField(
                "customer", query, operation, selection,
                schema.GetType<ObjectType>("Query"));

            // assert
            DocumentNode document = RemoteQueryBuilder.New()
                .SetRequestField(extractedField.Field)
                .AddFragmentDefinitions(extractedField.Fragments)
                .AddVariables(extractedField.Variables)
                .Build();

            QuerySyntaxSerializer.Serialize(document)
                    .MatchSnapshot();
        }

        private class DummyRewriter
            : QueryDelegationRewriterBase
        {
            public override FieldNode OnRewriteField(
                NameString targetSchemaName,
                IOutputType outputType,
                IOutputField outputField,
                FieldNode field)
            {
                return field.WithAlias(new NameNode("foo_bar"));
            }

            public override SelectionSetNode OnRewriteSelectionSet(
                NameString targetSchemaName,
                IOutputType outputType,
                IOutputField outputField,
                SelectionSetNode selectionSet)
            {
                return selectionSet.AddSelection(
                    new FieldNode
                    (
                        null,
                        new NameNode("abc_def"),
                        null,
                        Array.Empty<DirectiveNode>(),
                        Array.Empty<ArgumentNode>(),
                        null
                    ));
            }
        }
    }
}
