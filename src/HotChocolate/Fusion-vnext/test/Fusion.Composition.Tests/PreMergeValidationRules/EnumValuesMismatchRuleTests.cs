namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class EnumValuesMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EnumValuesMismatchRule();

    // In this example, both source schemas define "Genre" with the same value "FANTASY", satisfying
    // the rule.
    [Fact]
    public void Validate_NoEnumValuesMismatchSameValue_Succeeds()
    {
        AssertValid(
        [
            """
            enum Genre {
                FANTASY
            }
            """,
            """
            enum Genre {
                FANTASY
            }
            """
        ]);
    }

    // Here, the two definitions of "Genre" have shared values and additional values declared as
    // @inaccessible, satisfying the rule.
    [Fact]
    public void Validate_NoEnumValuesMismatchInaccessibleEnumValue_Succeeds()
    {
        AssertValid(
        [
            """
            enum Genre {
                FANTASY
                SCIENCE_FICTION @inaccessible
            }
            """,
            """
            enum Genre {
                FANTASY
            }
            """
        ]);
    }

    // Here, the two definitions of "Genre" have shared values in a differing order.
    [Fact]
    public void Validate_NoEnumValuesMismatchDifferingOrder_Succeeds()
    {
        AssertValid(
        [
            """
            enum Genre {
                FANTASY
                SCIENCE_FICTION @inaccessible
                ANIMATED
            }
            """,
            """
            enum Genre {
                ANIMATED
                FANTASY
                CRIME @inaccessible
            }
            """
        ]);
    }

    // Here, the two definitions of "Genre" have different values ("FANTASY" and "SCIENCE_FICTION"),
    // violating the rule.
    [Fact]
    public void Validate_EnumValuesMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                enum Genre {
                    FANTASY
                }
                """,
                """
                enum Genre {
                    SCIENCE_FICTION
                }
                """
            ],
            [
                "The enum type 'Genre' in schema 'A' must define the value 'SCIENCE_FICTION'.",
                "The enum type 'Genre' in schema 'B' must define the value 'FANTASY'."
            ]);
    }
}
