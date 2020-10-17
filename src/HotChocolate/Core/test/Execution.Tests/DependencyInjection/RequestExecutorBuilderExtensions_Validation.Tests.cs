using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types.Relay;
using HotChocolate.Validation;
using Xunit;

namespace HotChocolate.DependencyInjection
{
    public class RequestExecutorBuilderExtensionsValidationTests
    {
        public void Foo()
        {
            var service = new ServiceCollection()
                .AddGraphQL()
                .AddValidationVisitor<MockVisitor>();
        }

        public class MockVisitor : DocumentValidatorVisitor
        {
        }
    }
}
