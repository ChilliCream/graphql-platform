using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class ServiceArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public ServiceArgumentSourceCodeGeneratorTests()
            : base(new ServiceArgumentSourceCodeGenerator(),
                typeof(IAsyncLifetime),
                ArgumentKind.Service,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
