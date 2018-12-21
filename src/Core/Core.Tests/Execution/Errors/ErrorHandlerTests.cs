using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;

namespace Core.Tests.Execution.Errors
{
    public class ErrorHandlerTests
    {
        public async Task Foo()
        {
            // arrange
            ISchema schema = Schema.Create("type Query { foo: String }",
                c => c.BindResolver(
                    ctx =>
                    {
                        throw new Exception("Foo");
                    }).To("Query", "foo"));

            // act

            // assert
        }
    }
}
