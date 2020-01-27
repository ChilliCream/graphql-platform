using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;
using static HotChocolate.Execution.ArgumentNonNullValidator;

namespace HotChocolate.Execution
{
    public class ArgumentNonNullValidatorTests
    {
        [Fact]
        public void Validate_Input_With_Non_Null_Props_That_Have_No_Value_But_A_DefaultValue()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        test(bar: Bar): String
                    }

                    input Bar {
                        a: String! = ""bar""
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            IInputField field = schema.QueryType.Fields["test"].Arguments["bar"];

            // act
            Report report = ArgumentNonNullValidator.Validate(
                field,
                new ObjectValueNode(), Path.New("root"));

            // assert
            Assert.False(report.HasErrors);
        }

        [Fact]
        public void Validate_Input_With_Non_Null_Props_That_Have_No_Value_And_No_DefaultValue()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        test(bar: Bar): String
                    }

                    input Bar {
                        a: String!
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            IInputField field = schema.QueryType.Fields["test"].Arguments["bar"];

            // act
            Report report = ArgumentNonNullValidator.Validate(
                field,
                new ObjectValueNode(), Path.New("root"));

            // assert
            Assert.True(report.HasErrors);
            Assert.Equal("/root/a", report.Path.ToString());
        }
    }
}
