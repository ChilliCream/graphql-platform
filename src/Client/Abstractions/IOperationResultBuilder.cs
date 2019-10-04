using System.Collections.Generic;

namespace StrawberryShake
{
    public interface IOperationResultBuilder
    {
        IOperationResultBuilder AddError(IError error);
        IOperationResultBuilder AddErrors(IEnumerable<IError> errors);
        IOperationResultBuilder AddExtension(string key, object? value);
        IOperationResultBuilder AddExtensions(IEnumerable<KeyValuePair<string, object?>> extensions);
        IOperationResultBuilder ClearErrors();
        IOperationResultBuilder ClearExtension();
        IOperationResultBuilder RemoveExtension(string key);
        IOperationResultBuilder SetData(object data);
        IOperationResultBuilder SetExtension(string key, object? value);
        IOperationResultBuilder ClearAll();
    }
}
