using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public class ObjectTypeArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public ObjectTypeArgumentSourceCodeGeneratorTests()
            : base(new ObjectTypeArgumentSourceCodeGenerator(),
                typeof(ObjectType),
                ArgumentKind.ObjectType,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
