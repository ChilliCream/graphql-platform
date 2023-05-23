using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.Delegation;

public class RemoteQueryBuilderTests
{
    [Fact]
    public void BuildRemoteQuery()
    {
        // arrange
        var path =
            SelectionPathParser.Parse("a.b.c.d(a: $fields:bar)");

        var initialQuery =
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

        var field = initialQuery.Definitions
            .OfType<OperationDefinitionNode>().Single()
            .SelectionSet.Selections
            .OfType<FieldNode>().Single()
            .SelectionSet!.Selections
            .OfType<FieldNode>().Single();

        // act
        var newQuery = RemoteQueryBuilder.New()
            .SetOperation(null, OperationType.Query)
            .SetSelectionPath(path)
            .SetRequestField(field)
            .AddVariable("__fields_bar", new NamedTypeNode(null, new NameNode("String")))
            .Build("abc", new Dictionary<(string Type, string Schema), string>());

        // assert
        newQuery.Print().MatchSnapshot();
    }

    [Fact]
    public void BuildRemoteQueryCanOverrideOperationName()
    {
        // arrange
        var path =
            SelectionPathParser.Parse("a.b.c.d(a: $fields:bar)");

        var initialQuery =
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

        var field = initialQuery.Definitions
            .OfType<OperationDefinitionNode>().Single()
            .SelectionSet.Selections
            .OfType<FieldNode>().Single()
            .SelectionSet!.Selections
            .OfType<FieldNode>().Single();


        // act
        var newQuery = RemoteQueryBuilder.New()
            .SetOperation(new NameNode(
                    nameof(BuildRemoteQueryCanOverrideOperationName)),
                OperationType.Query)
            .SetSelectionPath(path)
            .SetRequestField(field)
            .AddVariable("__fields_bar", new NamedTypeNode(null, new NameNode("String")))
            .Build("abc", new Dictionary<(string Type, string Schema), string>());

        // assert
        newQuery.Print().MatchSnapshot();
    }
}
