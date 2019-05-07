using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Subscriptions;

namespace HotChocolate.Resolvers
{
    public class EventMessageArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public EventMessageArgumentSourceCodeGeneratorTests()
            : base(new EventMessageArgumentSourceCodeGenerator(),
                typeof(EventMessage),
                ArgumentKind.EventMessage,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
