using System.Collections.Immutable;
using HotChocolate;
using HotChocolate.Language;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;

namespace StrawberryShake.CodeGeneration;

internal static class ThrowHelper
{
    public static CodeGeneratorException DuplicateSelectionSet()
        => new CodeGeneratorException(
            new Error
            {
                Message = ThrowHelper_DuplicateSelectionSet,
                Extensions = ImmutableDictionary<string, object?>.Empty.SetItem("code", "SS0010")
            });

    public static CodeGeneratorException ReturnFragmentDoesNotExist()
        => new CodeGeneratorException(
            new Error
            {
                Message = ThrowHelper_ReturnFragmentDoesNotExist,
                Extensions = ImmutableDictionary<string, object?>.Empty.SetItem("code", "SS0011")
            });

    public static CodeGeneratorException FragmentMustBeImplementedByAllTypeFragments()
        => new CodeGeneratorException(
            new Error
            {
                Message = ThrowHelper_FragmentMustBeImplementedByAllTypeFragments,
                Extensions = ImmutableDictionary<string, object?>.Empty.SetItem("code", "SS0012")
            });

    public static CodeGeneratorException ResultTypeNameCollision(string resultTypeName)
        => new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_ResultTypeNameCollision, resultTypeName)
            .SetCode("SS0008")
            .Build());

    public static CodeGeneratorException TypeNameCollision(string typeName)
        => new(ErrorBuilder.New()
            .SetMessage(ThrowHelper_TypeNameCollision, typeName)
            .SetCode("SS0009")
            .Build());

    public static CodeGeneratorException UnionTypeDataEntityMixed(ISyntaxNode syntaxNode)
        => new(ErrorBuilder
            .New()
            .SetMessage(TypeDescriptorMapper_UnionTypeDataEntityMixed)
            .SetCode(CodeGenerationErrorCodes.UnionTypeDataEntityMixed)
            .AddLocation(syntaxNode)
            .Build());
}
