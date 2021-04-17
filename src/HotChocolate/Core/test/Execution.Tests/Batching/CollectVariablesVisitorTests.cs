using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.StarWars;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Batching
{
    public class CollectVariablesVisitorTests
    {
        [Fact]
        public void FindUndeclaredVariables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    hero(episode: $ep) {
                        name
                    }
                }");

            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var visitor = new CollectVariablesVisitor(schema);
            var visitationMap = new CollectVariablesVisitationMap();

            // act
            operation.Accept(
                visitor,
                visitationMap,
                _ => VisitorAction.Continue);

            // assert
            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);

            new DocumentNode(
                new IDefinitionNode[]
                {
                    operation
                }).Print().MatchSnapshot();

            Assert.Equal(0, visitor.ExportCount);
        }

        [Fact]
        public void FindUndeclaredVariablesInlineFragment()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    ... on Query {
                        hero(episode: $ep) {
                            name
                        }
                    }
                }");

            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var visitor = new CollectVariablesVisitor(schema);
            var visitationMap = new CollectVariablesVisitationMap();

            // act
            operation.Accept(
                visitor,
                visitationMap,
                node => VisitorAction.Continue);

            // assert
            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);

            new DocumentNode(
                new IDefinitionNode[]
                {
                    operation
                }).Print().MatchSnapshot();

            Assert.Equal(0, visitor.ExportCount);
        }

        [Fact]
        public void FindUndeclaredVariablesFragmentDefinition()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    ... q
                }

                fragment q on Query {
                    hero(episode: $ep) {
                        name
                    }
                }
                ");

            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var visitor = new CollectVariablesVisitor(schema);
            var visitationMap = new CollectVariablesVisitationMap();
            visitationMap.Initialize(
                document.Definitions.OfType<FragmentDefinitionNode>()
                    .ToDictionary(t => t.Name.Value));

            // act
            operation.Accept(
                visitor,
                visitationMap,
                _ => VisitorAction.Continue);

            // assert
            var variables = operation.VariableDefinitions.ToList();
            variables.AddRange(visitor.VariableDeclarations);
            operation = operation.WithVariableDefinitions(variables);

            var definitions = new List<IDefinitionNode>();
            definitions.Add(operation);
            definitions.AddRange(
                document.Definitions.OfType<FragmentDefinitionNode>());

            new DocumentNode(definitions).Print().MatchSnapshot();

            Assert.Equal(0, visitor.ExportCount);
        }


        [Fact]
        public void ExportCount()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                @"
                query getHero {
                    ... q
                }

                fragment q on Query {
                    hero(episode: $ep) @export(as: ""hero"") {
                        name @export(as: ""name"")
                        name2: name @export(as: ""name"")
                    }
                }
                ");

            OperationDefinitionNode operation = document.Definitions
                .OfType<OperationDefinitionNode>()
                .First();

            var visitor = new CollectVariablesVisitor(schema);
            var visitationMap = new CollectVariablesVisitationMap();
            visitationMap.Initialize(
                document.Definitions.OfType<FragmentDefinitionNode>()
                    .ToDictionary(t => t.Name.Value));

            // act
            operation.Accept(
                visitor,
                visitationMap,
                _ => VisitorAction.Continue);

            Assert.Equal(3, visitor.ExportCount);
        }

        [Fact]
        public void ConstructorExceptions()
        {
            Assert.Throws<ArgumentNullException>(() => new CollectVariablesVisitor(null));
        }
    }
}
