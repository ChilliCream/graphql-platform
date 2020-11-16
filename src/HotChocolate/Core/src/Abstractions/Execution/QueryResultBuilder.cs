using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public class QueryResultBuilder : IQueryResultBuilder
    {
        private IReadOnlyDictionary<string, object?>? _data;
        private List<IError>? _errors;
        private ExtensionData? _extensionData;
        private ExtensionData? _contextData;
        private string? _label;
        private Path? _path;
        private bool? _hasNext;
        private IDisposable? _disposable;

        public IQueryResultBuilder SetData(
            IReadOnlyDictionary<string, object?>? data,
            IDisposable? disposable = null)
        {
            _data = data;
            _disposable = disposable;
            return this;
        }

        public IQueryResultBuilder AddError(IError error)
        {
            if (error is null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.Add(error);
            return this;
        }

        public IQueryResultBuilder AddErrors(IEnumerable<IError> errors)
        {
            if (errors is null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            if (_errors is null)
            {
                _errors = new List<IError>();
            }

            _errors.AddRange(errors);
            return this;
        }

        public IQueryResultBuilder ClearErrors()
        {
            _errors = null;
            return this;
        }

        public IQueryResultBuilder AddExtension(string key, object? data)
        {
            if (_extensionData is null)
            {
                _extensionData = new ExtensionData();
            }

            _extensionData.Add(key, data);
            return this;
        }

        public IQueryResultBuilder SetExtension(string key, object? data)
        {
            if (_extensionData is null)
            {
                _extensionData = new ExtensionData();
            }

            _extensionData[key] = data;
            return this;
        }

        public IQueryResultBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions)
        {
            ClearExtensions();

            if (extensions is ExtensionData extensionData)
            {
                _extensionData = extensionData;
            }
            else if (extensions is { })
            {
                _extensionData = new ExtensionData(extensions);
            }

            return this;
        }

        public IQueryResultBuilder ClearExtensions()
        {
            _extensionData = null;
            return this;
        }

        public IQueryResultBuilder AddContextData(string key, object? data)
        {
            if (_contextData is null)
            {
                _contextData = new ExtensionData();
            }

            _contextData.Add(key, data);
            return this;
        }

        public IQueryResultBuilder SetContextData(string key, object? data)
        {
            if (_contextData is null)
            {
                _contextData = new ExtensionData();
            }

            _contextData[key] = data;
            return this;
        }

        public IQueryResultBuilder ClearContextData()
        {
            _contextData = null;
            return this;
        }

        public IQueryResultBuilder SetLabel(string? label)
        {
            _label = label;
            return this;
        }

        public IQueryResultBuilder SetPath(Path? path)
        {
            _path = path;
            return this;
        }

        public IQueryResultBuilder SetHasNext(bool? hasNext)
        {
            _hasNext = hasNext;
            return this;
        }

        public IQueryResult Create()
        {
            return new QueryResult(
                _data,
                _errors is { } && _errors.Count > 0 ? _errors : null,
                _extensionData is { } && _extensionData.Count > 0 ? _extensionData : null,
                _contextData is { } && _contextData.Count > 0 ? _contextData : null,
                _label,
                _path,
                _hasNext,
                _disposable);
        }

        public static QueryResultBuilder New() => new QueryResultBuilder();

        public static QueryResultBuilder FromResult(IQueryResult result)
        {
            var builder = new QueryResultBuilder();
            builder._data = result.Data;

            if (result.Errors is { })
            {
                builder._errors = new List<IError>(result.Errors);
            }

            if (result.Extensions is ExtensionData d)
            {
                builder._extensionData = new ExtensionData(d);
            }
            else if (result.Extensions is { })
            {
                builder._extensionData = new ExtensionData(result.Extensions);
            }

            return builder;
        }

        public static IQueryResult CreateError(
            IError error,
            IReadOnlyDictionary<string, object?>? contextData = null) =>
            new QueryResult(null, new List<IError> { error }, contextData: contextData);

        public static IQueryResult CreateError(
            IReadOnlyList<IError> errors,
            IReadOnlyDictionary<string, object?>? contextData = null) =>
            new QueryResult(null, errors, contextData: contextData);
    }
}
