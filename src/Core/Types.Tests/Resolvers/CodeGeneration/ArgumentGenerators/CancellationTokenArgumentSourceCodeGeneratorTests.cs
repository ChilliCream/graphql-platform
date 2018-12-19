using System.Threading;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class CancellationTokenArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public CancellationTokenArgumentSourceCodeGeneratorTests()
            : base(new CancellationTokenArgumentSourceCodeGenerator(),
                typeof(CancellationToken),
                ArgumentKind.CancellationToken,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
