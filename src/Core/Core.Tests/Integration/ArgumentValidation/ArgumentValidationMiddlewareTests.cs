using ChilliCream.Testing;
using HotChocolate.Execution;
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
            IExecutionResult result = schema.Execute("{ sayHello }");

            // assert
            result.Snapshot();
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
