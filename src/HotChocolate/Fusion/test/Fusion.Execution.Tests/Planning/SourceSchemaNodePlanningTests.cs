using HotChocolate.Fusion.Execution.Nodes.Serialization;

namespace HotChocolate.Fusion.Planning;

public sealed class SourceSchemaNodePlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ResolveEachNodeTypeThroughItsKeyOwner_When_KeyIsExternalInOtherNodeLookupSchema()
    {
        // arrange
        var schema = ComposeSchema(
            options =>
            {
                options.Merger.EnableGlobalObjectIdentification = true;
                options.Merger.NodeResolution = NodeResolution.SourceSchema;
            },
            """
            # name: a
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Account implements Node @key(fields: "id") {
              id: ID!
              username: String!
            }

            type Chat implements Node @key(fields: "id") {
              id: ID! @external
              account: Account!
            }
            """,
            """
            # name: b
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Account implements Node @key(fields: "id") {
              id: ID! @external
              chats: [Chat!]!
            }

            type Chat implements Node @key(fields: "id") {
              id: ID!
              text: String!
            }
            """);

        (string Name, string Operation)[] operations =
        [
            ("00-node-id",
                """
                query NodeId {
                  node(id: "a1") {
                    id
                  }
                }
                """),
            ("01-typenames",
                """
                query Typenames {
                  account: node(id: "a1") {
                    __typename
                  }
                  chat: node(id: "c1") {
                    __typename
                  }
                }
                """),
            ("02-member-fields",
                """
                query MemberFields {
                  account: node(id: "a1") {
                    ... on Account {
                      id
                      username
                    }
                  }
                  chat: node(id: "c1") {
                    ... on Chat {
                      id
                      text
                    }
                  }
                }
                """),
            ("03-other-member",
                """
                query OtherMember {
                  account: node(id: "a1") {
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("04-other-member-typename",
                """
                query OtherMemberTypename {
                  account: node(id: "a1") {
                    __typename
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("05-aliased",
                """
                query Aliased {
                  account: node(id: "a1") {
                    id
                    ... on Chat {
                      id
                    }
                  }
                  chat: node(id: "c1") {
                    __typename
                    ... on Account {
                      id
                    }
                  }
                }
                """),
            ("10-member-split",
                """
                query MemberSplit {
                  node(id: "a1") {
                    ... on Account {
                      id
                    }
                    ... on Chat {
                      id
                    }
                  }
                }
                """),
            ("20-node-id-chat",
                """
                query NodeIdChat {
                  node(id: "c1") {
                    id
                  }
                }
                """)
        ];

        var formatter = new YamlOperationPlanFormatter();
        var snapshot = Snapshot.Create();

        // act
        foreach (var (name, operation) in operations)
        {
            snapshot.Add(formatter.Format(PlanOperation(schema, operation)), name, MarkdownLanguages.Yaml);
        }

        // assert
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public void Plan_Should_ReenterRequiringSchemaWithKeyOfNodeBranch_When_KeyIsExternalInRequiredDataSchema()
    {
        // arrange
        var schema = ComposeSchema(
            options =>
            {
                options.Merger.EnableGlobalObjectIdentification = true;
                options.Merger.NodeResolution = NodeResolution.SourceSchema;
            },
            """
            # name: a
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Chat implements Node @key(fields: "id") {
              id: ID!
              title: String
              summary(text: String @require(field: "text")): String
            }
            """,
            """
            # name: b
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Chat implements Node @key(fields: "id") {
              id: ID! @external
              text: String!
            }
            """);

        (string Name, string Operation)[] operations =
        [
            ("00-requiring-field",
                """
                query RequiringField {
                  node(id: "c1") {
                    ... on Chat {
                      summary
                    }
                  }
                }
                """),
            ("01-key-and-requiring-field",
                """
                query KeyAndRequiringField {
                  node(id: "c1") {
                    ... on Chat {
                      id
                      summary
                    }
                  }
                }
                """),
            ("02-field-and-requiring-field",
                """
                query FieldAndRequiringField {
                  node(id: "c1") {
                    ... on Chat {
                      title
                      summary
                    }
                  }
                }
                """)
        ];

        var formatter = new YamlOperationPlanFormatter();
        var snapshot = Snapshot.Create();

        // act
        foreach (var (name, operation) in operations)
        {
            snapshot.Add(formatter.Format(PlanOperation(schema, operation)), name, MarkdownLanguages.Yaml);
        }

        // assert
        snapshot.MatchMarkdownSnapshot();
    }
}
