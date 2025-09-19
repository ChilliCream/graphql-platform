using HotChocolate;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.CSharp;

public class CSharpGeneratorResult
{
    public CSharpGeneratorResult()
        : this(
            Array.Empty<SourceDocument>(),
            Array.Empty<OperationType>(),
            Array.Empty<IError>())
    {
    }

    public CSharpGeneratorResult(
        IReadOnlyList<IError> errors)
        : this(
            Array.Empty<SourceDocument>(),
            Array.Empty<OperationType>(),
            errors)
    {
    }

    public CSharpGeneratorResult(
        IReadOnlyList<SourceDocument> documents,
        IReadOnlyList<OperationType> operationTypes)
        : this(documents, operationTypes, Array.Empty<IError>())
    {
    }

    public CSharpGeneratorResult(
        IReadOnlyList<SourceDocument> documents,
        IReadOnlyList<OperationType> operationTypes,
        IReadOnlyList<IError> errors)
    {
        Documents = documents;
        Errors = errors;
        OperationTypes = operationTypes;
    }

    public IReadOnlyList<OperationType> OperationTypes { get; }

    public IReadOnlyList<SourceDocument> Documents { get; }

    public IReadOnlyList<IError> Errors { get; }

    public bool HasErrors() => Errors.Count > 0;
}
