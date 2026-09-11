using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerCostDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @cost directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_CostDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @cost(weight: "1.0") {
                    field(argument: Int @cost(weight: "1.0")): Int @cost(weight: "1.0")
                }

                enum Enum @cost(weight: "1.0") {
                    VALUE
                }

                input Input {
                    field: Int @cost(weight: "1.0")
                }

                scalar Scalar @cost(weight: "1.0")

                {{s_costDirective}}
                """,
                $$"""
                # Schema B
                type Query @cost(weight: "1.0") {
                    field(argument: Int @cost(weight: "1.0")): Int @cost(weight: "1.0")
                }

                enum Enum @cost(weight: "1.0") {
                    VALUE
                }

                input Input {
                    field: Int @cost(weight: "1.0")
                }

                scalar Scalar @cost(weight: "1.0")

                {{s_costDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query
              @cost(weight: "1")
              @fusion__cost(schema: A, weight: "1.0")
              @fusion__cost(schema: B, weight: "1.0")
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              field(
                argument: Int @cost(weight: "1") @fusion__cost(schema: A, weight: "1.0") @fusion__cost(schema: B, weight: "1.0") @fusion__inputField(schema: A) @fusion__inputField(schema: B)
              ): Int
                @cost(weight: "1")
                @fusion__cost(schema: A, weight: "1.0")
                @fusion__cost(schema: B, weight: "1.0")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            input Input @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int @cost(weight: "1") @fusion__cost(schema: A, weight: "1.0") @fusion__cost(schema: B, weight: "1.0") @fusion__inputField(schema: A) @fusion__inputField(schema: B)
            }

            enum Enum
              @cost(weight: "1")
              @fusion__cost(schema: A, weight: "1.0")
              @fusion__cost(schema: B, weight: "1.0")
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              VALUE @fusion__enumValue(schema: A) @fusion__enumValue(schema: B)
            }

            scalar Scalar
              @cost(weight: "1")
              @fusion__cost(schema: A, weight: "1.0")
              @fusion__cost(schema: B, weight: "1.0")
              @fusion__type(schema: A)
              @fusion__type(schema: B)
            """,
            modifySchema: s_removeCostDirective);
    }

    // Do not merge @cost directives when the definitions do not match the canonical definition.
    [Fact]
    public void Merge_CostDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                scalar Foo @cost(level: 1.0)

                directive @cost(level: Float) repeatable on SCALAR
                """,
                """
                # Schema B
                scalar Foo @cost(index: 2)

                directive @cost(index: Int) on SCALAR
                """
            ],
            """
            scalar Foo @fusion__type(schema: A) @fusion__type(schema: B)
            """,
            modifySchema: s_removeCostDirective);
    }

    // Merge the maximum weight.
    [Fact]
    public void Merge_CostDirectivesMaxWeight_MatchesSnapshot()
    {
        AssertMatches(
            [
                $"""
                # Schema A
                scalar Foo @cost(weight: "-1.0")
                scalar Bar @cost(weight: "2.0")
                scalar Baz @cost(weight: "1.5")

                {s_costDirective}
                """,
                $"""
                # Schema B
                scalar Foo @cost(weight: "0.0")
                scalar Bar @cost(weight: "1.0")
                scalar Baz @cost(weight: "2.5")

                {s_costDirective}
                """
            ],
            """
            scalar Bar
                @cost(weight: "2")
                @fusion__cost(schema: A, weight: "2.0")
                @fusion__cost(schema: B, weight: "1.0")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Baz
                @cost(weight: "2.5")
                @fusion__cost(schema: A, weight: "1.5")
                @fusion__cost(schema: B, weight: "2.5")
                @fusion__type(schema: A)
                @fusion__type(schema: B)

            scalar Foo
                @cost(weight: "0")
                @fusion__cost(schema: A, weight: "-1.0")
                @fusion__cost(schema: B, weight: "0.0")
                @fusion__type(schema: A)
                @fusion__type(schema: B)
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence (R-COMPOSITION-WEIGHT-FOLD): a composite output field's
    // declared weight below the composite default (1) folds up to the default supplied by the
    // unannotated source.
    [Fact]
    public void Merge_CostDirectiveWeightFold_CompositeOutputField_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    book: Book @cost(weight: "-7")
                }

                type Book {
                    id: ID
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    book: Book
                }

                type Book {
                    id: ID
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              book: Book
                @cost(weight: "1")
                @fusion__cost(schema: A, weight: "-7")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            type Book @fusion__type(schema: A) @fusion__type(schema: B) {
              id: ID @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence: a leaf output field's declared weight within [0, 1) folds to
    // that value when the other source is unannotated (leaf default 0 does not raise it).
    [Fact]
    public void Merge_CostDirectiveWeightFold_LeafOutputFieldPositive_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @cost(weight: "0.5")
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    field: Int
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @cost(weight: "0.5")
                @fusion__cost(schema: A, weight: "0.5")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence: a leaf output field's declared negative weight folds up to the
    // leaf default (0) supplied by the unannotated source.
    [Fact]
    public void Merge_CostDirectiveWeightFold_LeafOutputFieldNegative_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @cost(weight: "-1")
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    field: Int
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @cost(weight: "0")
                @fusion__cost(schema: A, weight: "-1")
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence: a scalar-typed argument's declared negative weight folds up to
    // the argument default (0) supplied by the unannotated source.
    [Fact]
    public void Merge_CostDirectiveWeightFold_ScalarArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field(argument: Int @cost(weight: "-1")): Int
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    field(argument: Int): Int
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field(
                argument: Int @cost(weight: "0") @fusion__cost(schema: A, weight: "-1") @fusion__inputField(schema: A) @fusion__inputField(schema: B)
              ): Int @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence: an input-object-typed argument's declared negative weight
    // folds up to the input-object default (1) supplied by the unannotated source.
    [Fact]
    public void Merge_CostDirectiveWeightFold_InputObjectArgument_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field(argument: Filter @cost(weight: "-1")): Int
                }

                input Filter {
                    name: String
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    field(argument: Filter): Int
                }

                input Filter {
                    name: String
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field(
                argument: Filter @cost(weight: "1") @fusion__cost(schema: A, weight: "-1") @fusion__inputField(schema: A) @fusion__inputField(schema: B)
              ): Int @fusion__field(schema: A) @fusion__field(schema: B)
            }

            input Filter @fusion__type(schema: A) @fusion__type(schema: B) {
              name: String @fusion__inputField(schema: A) @fusion__inputField(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    // Weight fold under absence: an object type's declared weight below the composite default
    // (1) folds up to the default supplied by the unannotated source.
    [Fact]
    public void Merge_CostDirectiveWeightFold_ObjectType_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    book: Book
                }

                type Book @cost(weight: "-3") {
                    id: ID
                }

                {{s_costDirective}}
                """,
                """
                # Schema B
                type Query {
                    book: Book
                }

                type Book {
                    id: ID
                }
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              book: Book @fusion__field(schema: A) @fusion__field(schema: B)
            }

            type Book
              @cost(weight: "1")
              @fusion__cost(schema: A, weight: "-3")
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              id: ID @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeCostDirective);
    }

    private static readonly CostMutableDirectiveDefinition s_costDirective
        = new(BuiltIns.String.Create());

    private static readonly Action<MutableSchemaDefinition> s_removeCostDirective
        = schema => schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.Cost);
}
