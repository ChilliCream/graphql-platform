using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;

namespace StrawberryShake.CodeGeneration
{
    public static class CodeGenerationThrowHelper
    {
        public const string FileExtensionKey = "file";
        public static IReadOnlyList<IError> Generator_NoExecutableDocumentsFound() =>
            new CodeGeneratorException(Throwhelper_Generator_NoExecutableDocumentsFound).Errors;

        public static IReadOnlyList<IError> Generator_NoGraphQLFilesFound() =>
            new CodeGeneratorException(Throwhelper_Generator_NoGraphQLFilesFound).Errors;

        public static IReadOnlyList<IError> Generator_NoTypeDocumentsFound() =>
            new CodeGeneratorException(Throwhelper_Generator_NoTypeDocumentsFound).Errors;

        public static IError Generator_SyntaxException(SyntaxException syntaxException, string file) =>
            ErrorBuilder.New()
                .SetExtension(FileExtensionKey, file)
                .SetMessage(Throwhelper_Generator_SyntaxError)
                .AddLocation(new HotChocolate.Location(
                    syntaxException.Line,
                    syntaxException.Column))
                .Build();
    }
}
