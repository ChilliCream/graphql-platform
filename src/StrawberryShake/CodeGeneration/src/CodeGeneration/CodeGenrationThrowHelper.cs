using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;
using static StrawberryShake.CodeGeneration.CodeGenerationErrorCodes;

namespace StrawberryShake.CodeGeneration
{
    public static class CodeGenerationThrowHelper
    {
        public const string FileExtensionKey = "file";
        public const string TitleExtensionKey = "title";
        public static IReadOnlyList<IError> Generator_NoExecutableDocumentsFound() =>
            new CodeGeneratorException(Throwhelper_Generator_NoExecutableDocumentsFound).Errors
                .Select(error => error
                    .WithExtensions(new Dictionary<string, object?>()
                    {
                        {FileExtensionKey, "No executable documents were found"}
                    })
                    .WithCode(NoExecutableDocumentsFound)).ToList();

        public static IReadOnlyList<IError> Generator_NoTypeDocumentsFound() =>
            new CodeGeneratorException(Throwhelper_Generator_NoTypeDocumentsFound).Errors
                .Select(error => error
                    .WithExtensions(new Dictionary<string, object?>()
                    {
                        {TitleExtensionKey, "No type documents were found"}
                    }).WithCode(NoTypeDocumentsFound)).ToList();

        public static IError Generator_SyntaxException(SyntaxException syntaxException, string file) =>
            ErrorBuilder.New()
                .SetCode(SyntaxError)
                .SetExtension(FileExtensionKey, file)
                .SetMessage(Throwhelper_Generator_SyntaxError)
                .SetException(syntaxException)
                .SetExtension(TitleExtensionKey, "A graphql file contains a syntax error")
                .AddLocation(new HotChocolate.Location(
                    syntaxException.Line,
                    syntaxException.Column))
                .Build();
    }
}
