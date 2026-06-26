using HotChocolate;
using HotChocolate.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static StrawberryShake.CodeGeneration.CodeGenerationErrorCodes;
using Location = HotChocolate.Location;

namespace StrawberryShake.CodeGeneration;

public static class ErrorHelper
{
    public const string FileExtensionKey = "file";
    public const string TitleExtensionKey = "title";

    public static IError WithFileReference(
        this IError error,
#pragma warning disable RCS1163
        IDictionary<ISyntaxNode, string> fileLookup)
#pragma warning restore RCS1163
    {
        var extensions = ImmutableOrderedDictionary<string, object?>.Empty
            .Add(TitleExtensionKey, "Schema validation error")
            .Add("code", SchemaValidationError);

        // TODO : we need to bring Mutable in and re-enable this.
        // if the error has a syntax node we will try to lookup the
        // document and add the filename to the error.
        // if (error is Error { SyntaxNode: { } node } &&
        //    fileLookup.TryGetValue(node, out var filename))
        // {
        //    extensions.Add(FileExtensionKey, filename);
        // }

        return error.WithExtensions(extensions);
    }

    public static IError SchemaError(this SchemaInitializationException exception) =>
        ErrorBuilder.New()
            .SetMessage(exception.Message ?? "The GraphQL schema could not be initialized.")
            .SetCode(SchemaValidationError)
            .SetException(exception)
            .SetExtension(TitleExtensionKey, "Schema validation error")
            .Build();

    public static IError SyntaxError(
        this SyntaxException exception,
        string file) =>
        ErrorBuilder.New()
            .SetMessage(exception.Message)
            .SetExtension(FileExtensionKey, file)
            .SetException(exception)
            .AddLocation(new Location(exception.Line, exception.Column))
            .SetCode(CodeGenerationErrorCodes.SyntaxError)
            .Build();
}
