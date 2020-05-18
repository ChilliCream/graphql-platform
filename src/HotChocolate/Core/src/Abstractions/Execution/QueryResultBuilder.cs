using System;
using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution
{
    public class QueryResultBuilder
        : IQueryResultBuilder
    {
        private IReadOnlyDictionary<string, object?>? _data;
        private List<IError>? _errors;
        private ExtensionData? _extensionData;
        private ExtensionData? _contextData;

        public IQueryResultBuilder SetData(IReadOnlyDictionary<string, object?>? data)
        {
            _data = data;
            return this;
        }

        public IQueryResultBuilder SetData(IResult result)
        {
            throw new NotImplementedException();
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

        public IReadOnlyQueryResult Create()
        {
            return new ReadOnlyQueryResult(
                _data,
                _errors is { } && _errors.Count > 0 ? _errors : null,
                _extensionData is { } && _extensionData.Count > 0 ? _extensionData : null,
                _contextData is { } && _contextData.Count > 0 ? _contextData : null);
        }

        public static QueryResultBuilder New() => new QueryResultBuilder();

        public static QueryResultBuilder FromResult(IReadOnlyQueryResult result)
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

        public static IReadOnlyQueryResult CreateError(IError error) =>
            new ReadOnlyQueryResult(null, new List<IError> { error }, null, null);

        public static IReadOnlyQueryResult CreateError(IEnumerable<IError> errors) =>
            new ReadOnlyQueryResult(null, new List<IError>(errors), null, null);
    }
}
