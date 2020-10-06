using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class RemoteQueryBuilderTests
    {
        [Fact]
        public void BuildRemoteQuery()
        {
            // arrange
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse("a.b.c.d(a: $fields:bar)");

            DocumentNode initialQuery =
                Utf8GraphQLParser.Parse(
                    @"{
                        foo {
                          bar {
                            baz {
                              ... on Baz {
                                qux
                              }
                            }
                          }
                        }
                      }
                    ");

            FieldNode field = initialQuery.Definitions
                .OfType<OperationDefinitionNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single();


            // act
            DocumentNode newQuery = RemoteQueryBuilder.New()
                .SetOperation(null, OperationType.Query)
                .SetSelectionPath(path)
                .SetRequestField(field)
                .AddVariable("fields_bar",
                    new NamedTypeNode(null, new NameNode("String")))
                .Build();

            // assert
            QuerySyntaxSerializer.Serialize(newQuery).MatchSnapshot();
        }

        [Fact]
        public void BuildRemoteQueryCanOverrideOperationName()
        {
            // arrange
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse("a.b.c.d(a: $fields:bar)");

            DocumentNode initialQuery =
                Utf8GraphQLParser.Parse(
                    @"{
                        foo {
                          bar {
                            baz {
                              ... on Baz {
                                qux
                              }
                            }
                          }
                        }
                      }
                    ");

            FieldNode field = initialQuery.Definitions
                .OfType<OperationDefinitionNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single()
                .SelectionSet.Selections
                .OfType<FieldNode>().Single();


            // act
            DocumentNode newQuery = RemoteQueryBuilder.New()
                .SetOperation(new NameNode(nameof(BuildRemoteQueryCanOverrideOperationName)), OperationType.Query)
                .SetSelectionPath(path)
                .SetRequestField(field)
                .AddVariable("fields_bar",
                    new NamedTypeNode(null, new NameNode("String")))
                .Build();

            // assert
            QuerySyntaxSerializer.Serialize(newQuery).MatchSnapshot();
        }
    }
}
