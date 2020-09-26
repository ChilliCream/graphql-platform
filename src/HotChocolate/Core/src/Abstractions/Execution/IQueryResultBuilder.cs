using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public interface IQueryResultBuilder
    {
        IQueryResultBuilder SetData(
            IReadOnlyDictionary<string, object?>? data,
            IDisposable? disposable = null);

        IQueryResultBuilder AddError(IError error);

        IQueryResultBuilder AddErrors(IEnumerable<IError> errors);

        IQueryResultBuilder AddExtension(string key, object? data);

        IQueryResultBuilder SetExtension(string key, object? data);

        IQueryResultBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions);

        IQueryResultBuilder AddContextData(string key, object? data);

        IQueryResultBuilder SetContextData(string key, object? data);

        IQueryResultBuilder ClearContextData();

        IQueryResultBuilder SetLabel(string? label);

        IQueryResultBuilder SetPath(Path? path);

        IQueryResultBuilder SetHasNext(bool? hasNext);

        IQueryResult Create();
    }
}
