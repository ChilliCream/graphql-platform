using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public class ThrowHelper
    {
        public static CodeGeneratorException DuplicateSelectionSet() =>
            new(new Error(
                "The same selection set is mapped multiple times.",
                "SS0010"));
    }
}
