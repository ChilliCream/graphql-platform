using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.Directives
{
    public class DirectiveIntegrationTests
    {
        [Fact]
        public void UniqueDirectives_OnFieldLevel_OverwriteOnesOnObjectLevel()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(
                "{ bar baz }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void UniqueDirectives_FieldSelection_OverwriteTypeSystemOnes()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(
                "{ bar baz @constant(value: \"baz\") }");

            // assert
            result.MatchSnapshot();
        }

        private static ISchema CreateSchema()
        {
            return Schema.Create(
                "type Query @constant(value: \"foo\") " +
                "{ bar: String baz: String @constant(value: \"bar\")  }",
                c => c.RegisterDirective<ConstantDirectiveType>());
        }
    }
}
