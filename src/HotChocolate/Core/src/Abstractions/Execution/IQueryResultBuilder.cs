using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IQueryResultBuilder
    {
        IQueryResultBuilder SetData(IReadOnlyDictionary<string, object?>? data);

        IQueryResultBuilder AddError(IError error);

        IQueryResultBuilder AddErrors(IEnumerable<IError> errors);

        IQueryResultBuilder AddExtension(string key, object? data);

        IQueryResultBuilder SetExtension(string key, object? data);

        IQueryResultBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions);

        IQueryResultBuilder AddContextData(string key, object? data);

        IQueryResultBuilder SetContextData(string key, object? data);

        IQueryResultBuilder ClearContextData();

        IReadOnlyQueryResult Create();
    }
}
