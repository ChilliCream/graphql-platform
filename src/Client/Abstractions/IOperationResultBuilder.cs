using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResultBuilder
    {
        object? Data { get; }

        IReadOnlyList<IError>? Errors { get; }

        IReadOnlyDictionary<string, object?>? Extensions { get; }

        bool IsDataOrErrorModified { get; }

        IOperationResultBuilder AddError(IError error);

        IOperationResultBuilder AddErrors(IEnumerable<IError> errors);

        IOperationResultBuilder AddExtension(string key, object? value);

        IOperationResultBuilder AddExtensions(
            IEnumerable<KeyValuePair<string, object?>> extensions);

        IOperationResultBuilder ClearErrors();

        IOperationResultBuilder ClearExtensions();

        IOperationResultBuilder RemoveExtension(string key);

        IOperationResultBuilder SetData(object data);

        IOperationResultBuilder SetExtension(string key, object? value);

        IOperationResultBuilder ClearAll();

        IOperationResult Build();
    }
}
