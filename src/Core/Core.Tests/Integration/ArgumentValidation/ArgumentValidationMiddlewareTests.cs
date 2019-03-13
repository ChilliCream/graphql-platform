using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ArgumentValidationMiddlewareTests
    {
        [Fact]
        public void NameIsNotNull()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(
                "{ sayHello }");

            // assert
            result.MatchSnapshot();
        }

        private static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterDirective<ArgumentValidationDirectiveType>();
                c.RegisterDirective<ExecuteArgumentValidationDirectiveType>();
            });
        }
    }
}
