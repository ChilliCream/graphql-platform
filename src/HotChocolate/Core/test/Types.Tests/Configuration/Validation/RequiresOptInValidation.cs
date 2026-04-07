namespace HotChocolate.Configuration.Validation;

public class RequiresOptInValidation : TypeValidationTestBase
{
    [Fact]
    public void Must_Not_Appear_On_Required_Input_Object_Field()
    {
        ExpectError(
            """
            input Input {
                field: Int!
                    @requiresOptIn(feature: "feature1")
                    @requiresOptIn(feature: "feature2")
            }
            """);
    }

    [Fact]
    public void May_Appear_On_Required_With_Default_Input_Object_Field()
    {
        ExpectValid(
            """
            type Query { field: Int }

            input Input {
                field: Int! = 1 @requiresOptIn(feature: "feature")
            }
            """);
    }

    [Fact]
    public void May_Appear_On_Nullable_Input_Object_Field()
    {
        ExpectValid(
            """
            type Query { field: Int }

            input Input {
                field: Int @requiresOptIn(feature: "feature")
            }
            """);
    }

    [Fact]
    public void Must_Not_Appear_On_Required_Argument()
    {
        ExpectError(
            """
            type Object {
                field(
                    argument: Int!
                        @requiresOptIn(feature: "feature1")
                        @requiresOptIn(feature: "feature2")): Int
            }
            """);
    }

    [Fact]
    public void May_Appear_On_Required_With_Default_Argument()
    {
        ExpectValid(
            """
            type Query { field: Int }

            type Object {
                field(argument: Int! = 1 @requiresOptIn(feature: "feature")): Int
            }
            """);
    }

    [Fact]
    public void May_Appear_On_Nullable_Argument()
    {
        ExpectValid(
            """
            type Query { field: Int }

            type Object {
                field(argument: Int @requiresOptIn(feature: "feature")): Int
            }
            """);
    }
}
