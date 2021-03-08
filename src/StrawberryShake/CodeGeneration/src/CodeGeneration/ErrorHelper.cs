using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using SyntaxVisitor = HotChocolate.Language.Visitors.SyntaxVisitor;
using static StrawberryShake.CodeGeneration.Properties.CodeGenerationResources;
using static StrawberryShake.CodeGeneration.CodeGenerationErrorCodes;

namespace StrawberryShake.CodeGeneration
{
    internal static class ErrorHelper
    {
        public const string FileExtensionKey = "file";
        public const string TitleExtensionKey = "title";

        public static IError WithFileReference(
            this IError error,
            IDictionary<ISyntaxNode, string> fileLookup)
        {
            var extensions = new Dictionary<string, object?>();
            extensions.Add(TitleExtensionKey, "Schema validation error");

            // if the error has a syntax node we will try to lookup the
            // document and add the filename to the error.
            if (error is Error { SyntaxNode: { } node } &&
                fileLookup.TryGetValue(node, out var filename))
            {
                extensions.Add(FileExtensionKey, filename);
            }

            return error
                .WithCode(SchemaValidationError)
                .WithExtensions(extensions);
        }

        public static IError SchemaError(
            this ISchemaError error,
            IDictionary<ISyntaxNode, string> fileLookup)
        {
            var builder = ErrorBuilder.New();

            foreach (var extension in error.Extensions)
            {
                builder.SetExtension(extension.Key, extension.Value);
            }

            builder.SetExtension(TitleExtensionKey, "Schema validation error");

            foreach (ISyntaxNode syntaxNode in error.SyntaxNodes)
            {
                // if the error has a syntax node we will try to lookup the
                // document and add the filename to the error.
                if (fileLookup.TryGetValue(syntaxNode, out var filename))
                {
                    builder.SetExtension(FileExtensionKey, filename);
                }
            }

            return builder
                .SetMessage(error.Message)
                .SetCode(error.Code)
                .SetException(error.Exception)
                .Build();
        }

        public static IError SyntaxError(
            this SyntaxException exception,
            string file) =>
            ErrorBuilder.New()
                .SetMessage(exception.Message)
                .SetExtension(FileExtensionKey, file)
                .SetException(exception)
                .AddLocation(exception.Line, exception.Column)
                .SetCode(CodeGenerationErrorCodes.SyntaxError)
                .Build();
    }
}
