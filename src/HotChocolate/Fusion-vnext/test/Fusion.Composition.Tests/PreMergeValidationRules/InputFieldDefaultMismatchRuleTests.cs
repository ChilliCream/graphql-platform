namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InputFieldDefaultMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InputFieldDefaultMismatchRule();

    // In the following example both source schemas have an input field "genre" with the same
    // default value. This is valid.
    [Fact]
    public void Validate_InputFieldDefaultMatchSameDefaultValue_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                genre: Genre = FANTASY
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """,
            """
            # Schema B
            input BookFilter {
                genre: Genre = FANTASY
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """
        ]);
    }

    // If only one of the source schemas defines a default value for a given input field, the
    // composition is still valid.
    [Fact]
    public void Validate_InputFieldDefaultMatchOneDefaultValue_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                genre: Genre
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """,
            """
            # Schema B
            input BookFilter {
                genre: Genre = FANTASY
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """
        ]);
    }

    // Multiple input fields.
    [Fact]
    public void Validate_InputFieldDefaultMatchMultipleInputFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            input BookFilter {
                genre1: Genre = FANTASY
                genre2: Genre
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """,
            """
            # Schema B
            input BookFilter {
                genre1: Genre
                genre2: Genre = SCIENCE_FICTION
            }

            enum Genre {
                FANTASY
                SCIENCE_FICTION
            }
            """
        ]);
    }

    // In the following example both source schemas define an input field "minPageCount" with
    // different default values. This is invalid.
    [Fact]
    public void Validate_InputFieldDefaultMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    minPageCount: Int = 10
                }
                """,
                """
                # Schema B
                input BookFilter {
                    minPageCount: Int = 20
                }
                """
            ],
            [
                "The default value '10' of input field 'BookFilter.minPageCount' in schema 'A' "
                + "differs from the default value of '20' in schema 'B'."
            ]);
    }

    // Two different default values, and one without a default value.
    [Fact]
    public void Validate_InputFieldDefaultMismatchTwoDifferentOneWithout_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    minPageCount: Int = 10
                }
                """,
                """
                # Schema B
                input BookFilter {
                    minPageCount: Int
                }
                """,
                """
                # Schema C
                input BookFilter {
                    minPageCount: Int = 20
                }
                """
            ],
            [
                "The default value '10' of input field 'BookFilter.minPageCount' in schema 'A' "
                + "differs from the default value of '20' in schema 'C'."
            ]);
    }

    // Three different default values.
    [Fact]
    public void Validate_InputFieldDefaultMismatchThreeDifferent_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                input BookFilter {
                    minPageCount: Int = 10
                }
                """,
                """
                # Schema B
                input BookFilter {
                    minPageCount: Int = 20
                }
                """,
                """
                # Schema C
                input BookFilter {
                    minPageCount: Int = 30
                }
                """
            ],
            [
                "The default value '10' of input field 'BookFilter.minPageCount' in schema 'A' "
                + "differs from the default value of '20' in schema 'B'.",

                "The default value '20' of input field 'BookFilter.minPageCount' in schema 'B' "
                + "differs from the default value of '30' in schema 'C'."
            ]);
    }
}
