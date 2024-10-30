using HotChocolate;
using HotChocolate.Language;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;

namespace StrawberryShake.CodeGeneration;

internal static class ThrowHelper
{
    public static CodeGeneratorException DuplicateSelectionSet() =>
        new(new Error(ThrowHelper_DuplicateSelectionSet, "SS0010"));

    public static CodeGeneratorException ReturnFragmentDoesNotExist() =>
        new(new Error(ThrowHelper_ReturnFragmentDoesNotExist, "SS0011"));

    public static CodeGeneratorException FragmentMustBeImplementedByAllTypeFragments() =>
        new(new Error(ThrowHelper_FragmentMustBeImplementedByAllTypeFragments, "SS0012"));

    public static CodeGeneratorException ResultTypeNameCollision(string resultTypeName) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_ResultTypeNameCollision, resultTypeName)
            .SetCode("SS0008")
            .Build());

    public static CodeGeneratorException TypeNameCollision(string typeName) =>
        new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_TypeNameCollision, typeName)
            .SetCode("SS0009")
            .Build());

    public static CodeGeneratorException UnionTypeDataEntityMixed(
        ISyntaxNode syntaxNode) =>
        new(ErrorBuilder
            .New()
            .SetMessage(TypeDescriptorMapper_UnionTypeDataEntityMixed)
            .SetCode(CodeGenerationErrorCodes.UnionTypeDataEntityMixed)
            .AddLocation(syntaxNode)
            .Build());
}
