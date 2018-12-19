using ChilliCream.Testing;
using HotChocolate.Execution;
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
            IExecutionResult result = schema.Execute("{ bar baz }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public void UniqueDirectives_FieldSelection_OverwriteTypeSystemOnes()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute(
                "{ bar baz @constant(value: \"baz\") }");

            // assert
            result.Snapshot();
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
