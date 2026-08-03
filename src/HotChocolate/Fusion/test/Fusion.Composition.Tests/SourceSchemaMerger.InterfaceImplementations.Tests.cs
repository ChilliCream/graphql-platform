namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerInterfaceImplementationsTests : SourceSchemaMergerTestBase
{
    // Two source schemas each declare one unrelated interface on the shared "Chair" type. Neither
    // interface implements the other, so composition unions the two declarations.
    [Fact]
    public void Merge_UnrelatedInterfacesUnioned_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                interface Product {
                    id: ID!
                }

                type Chair implements Product @key(fields: "id") {
                    id: ID!
                    legs: Int
                }
                """,
                """
                # Schema B
                interface Searchable {
                    score: Float
                }

                type Chair implements Searchable @key(fields: "id") {
                    id: ID!
                    score: Float
                }
                """
            ],
            """
            type Chair implements Product & Searchable
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Product")
              @fusion__implements(schema: B, interface: "Searchable") {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              legs: Int @fusion__field(schema: A)
              score: Float @fusion__field(schema: B)
            }

            interface Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }

            interface Searchable @fusion__type(schema: B) {
              score: Float @fusion__field(schema: B)
            }
            """);
    }
}
