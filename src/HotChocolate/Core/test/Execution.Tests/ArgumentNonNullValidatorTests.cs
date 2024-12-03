using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution;

public class ArgumentNonNullValidatorTests
{
    [Fact]
    public void Validate_Input_With_Non_Null_Props_That_Have_No_Value_But_A_DefaultValue()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(@"
                    type Query {
                        test(bar: Bar): String
                    }

                    input Bar {
                        a: String! = ""bar""
                    }
                ")
            .Use(_ => _ => default)
            .Create();

        IInputField field = schema.QueryType.Fields["test"].Arguments["bar"];

        // act
        var report =
            ArgumentNonNullValidator.Validate(
                field,
                new ObjectValueNode(), Path.Root.Append("root"));

        // assert
        Assert.False(report.HasErrors);
    }

    [Fact]
    public void Validate_Input_With_Non_Null_Props_That_Have_No_Value_And_No_DefaultValue()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(@"
                    type Query {
                        test(bar: Bar): String
                    }

                    input Bar {
                        a: String!
                    }
                ")
            .Use(_ => _ => default)
            .Create();

        IInputField field = schema.QueryType.Fields["test"].Arguments["bar"];

        // act
        var report = ArgumentNonNullValidator.Validate(
            field,
            new ObjectValueNode(), Path.Root.Append("root"));

        // assert
        Assert.True(report.HasErrors);
        Assert.Equal("/root/a", report.Path.ToString());
    }
}
