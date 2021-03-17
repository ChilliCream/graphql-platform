using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Execution
{
    public class QueryRequestBuilder : IQueryRequestBuilder
    {
        private IQuery? _query;
        private string? _queryName;
        private string? _queryHash;
        private string? _operationName;
        private IReadOnlyDictionary<string, object?>? _readOnlyVariableValues;
        private Dictionary<string, object?>? _variableValuesDict;
        private object? _initialValue;
        private IReadOnlyDictionary<string, object?>? _readOnlyProperties;
        private Dictionary<string, object?>? _properties;
        private IReadOnlyDictionary<string, object?>? _readOnlyExtensions;
        private Dictionary<string, object?>? _extensions;
        private IServiceProvider? _services;
        private OperationType[]? _allowedOperations;

        public IQueryRequestBuilder SetQuery(string sourceText)
        {
            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNullOrEmpty,
                    nameof(sourceText));
            }

            _query = new QuerySourceText(sourceText);
            return this;
        }

        public IQueryRequestBuilder SetQuery(DocumentNode document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _query = new QueryDocument(document);
            return this;
        }

        public IQueryRequestBuilder SetQueryId(string? queryName)
        {
            _queryName = queryName;
            return this;
        }

        public IQueryRequestBuilder SetQueryHash(string? queryHash)
        {
            _queryHash = queryHash;
            return this;
        }

        public IQueryRequestBuilder SetOperation(string? operationName)
        {
            _operationName = operationName;
            return this;
        }

        public IQueryRequestBuilder SetInitialValue(object? initialValue)
        {
            _initialValue = initialValue;
            return this;
        }

        public IQueryRequestBuilder SetServices(
            IServiceProvider? services)
        {
            _services = services;
            return this;
        }

        public IQueryRequestBuilder TrySetServices(
            IServiceProvider? services)
        {
            _services ??= services;
            return this;
        }

        public IQueryRequestBuilder SetAllowedOperations(
            OperationType[]? allowedOperations)
        {
            _allowedOperations = allowedOperations;
            return this;
        }

        public IQueryRequestBuilder SetVariableValues(
            Dictionary<string, object?>? variableValues) =>
            SetVariableValues((IDictionary<string, object?>)variableValues);


        public IQueryRequestBuilder SetVariableValues(
            IDictionary<string, object?>? variableValues)
        {
            _variableValuesDict = variableValues is null
                ? null
                : new Dictionary<string, object?>(variableValues);
            _readOnlyVariableValues = null;
            return this;
        }

        public IQueryRequestBuilder SetVariableValues(
           IReadOnlyDictionary<string, object?>? variableValues)
        {
            _variableValuesDict = null;
            _readOnlyVariableValues = variableValues;
            return this;
        }

        public IQueryRequestBuilder SetVariableValue(string name, object? value)
        {
            InitializeVariables();

            _variableValuesDict[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddVariableValue(
            string name, object? value)
        {
            InitializeVariables();

            _variableValuesDict.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddVariableValue(
            string name, object? value)
        {
            InitializeVariables();

            if (!_variableValuesDict.ContainsKey(name))
            {
                _variableValuesDict.Add(name, value);
            }
            return this;
        }

        public IQueryRequestBuilder SetProperties(
            Dictionary<string, object?>? properties) =>
            SetProperties((IDictionary<string, object?>?)properties);


        public IQueryRequestBuilder SetProperties(
            IDictionary<string, object?>? properties)
        {
            _properties = properties is null
                ? null
                : new Dictionary<string, object?>(properties);
            _readOnlyProperties = null;
            return this;
        }

        public IQueryRequestBuilder SetProperties(
            IReadOnlyDictionary<string, object?>? properties)
        {
            _properties = null;
            _readOnlyProperties = properties;
            return this;
        }

        public IQueryRequestBuilder SetProperty(string name, object? value)
        {
            InitializeProperties();

            _properties[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddProperty(
            string name, object? value)
        {
            InitializeProperties();

            _properties.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddProperty(
            string name, object? value)
        {
            InitializeProperties();

            if (!_properties.ContainsKey(name))
            {
                _properties.Add(name, value);
            }
            return this;
        }

        public IQueryRequestBuilder SetExtensions(
            Dictionary<string, object?>? extensions) =>
            SetExtensions((IDictionary<string, object?>?)extensions);

        public IQueryRequestBuilder SetExtensions(
            IDictionary<string, object?>? extensions)
        {
            _extensions = extensions is null
                ? null
                : new Dictionary<string, object?>(extensions);
            _readOnlyExtensions = null;
            return this;
        }

        public IQueryRequestBuilder SetExtensions(
            IReadOnlyDictionary<string, object?>? extensions)
        {
            _extensions = null;
            _readOnlyExtensions = extensions;
            return this;
        }

        public IQueryRequestBuilder SetExtension(string name, object? value)
        {
            InitializeExtensions();

            _extensions[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddExtension(
            string name, object? value)
        {
            InitializeExtensions();

            _extensions.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddExtension(
            string name, object? value)
        {
            InitializeExtensions();

            if (!_extensions.ContainsKey(name))
            {
                _extensions.Add(name, value);
            }
            return this;
        }

        public IReadOnlyQueryRequest Create()
        {
            return new QueryRequest
            (
                query: _query,
                queryId: _queryName,
                queryHash: _queryHash,
                operationName: _operationName,
                initialValue: _initialValue,
                services: _services,
                variableValues: GetVariableValues(),
                contextData: GetProperties(),
                extensions: GetExtensions(),
                allowedOperations: _allowedOperations
            );
        }

        private IReadOnlyDictionary<string, object?> GetVariableValues()
        {
            return _variableValuesDict ?? _readOnlyVariableValues;
        }

        private void InitializeVariables()
        {
            if (_variableValuesDict is null)
            {
                _variableValuesDict = _readOnlyVariableValues is null
                    ? new Dictionary<string, object?>()
                    : _readOnlyVariableValues.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyVariableValues = null;
            }
        }

        private IReadOnlyDictionary<string, object?> GetProperties()
        {
            return _properties ?? _readOnlyProperties;
        }

        private void InitializeProperties()
        {
            if (_properties is null)
            {
                _properties = _readOnlyProperties is null
                    ? new Dictionary<string, object?>()
                    : _readOnlyProperties.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyProperties = null;
            }
        }

        private IReadOnlyDictionary<string, object?> GetExtensions()
        {
            return _extensions ?? _readOnlyExtensions;
        }

        private void InitializeExtensions()
        {
            if (_extensions is null)
            {
                _extensions = _readOnlyExtensions is null
                    ? new Dictionary<string, object?>()
                    : _readOnlyExtensions.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyExtensions = null;
            }
        }

        public static IReadOnlyQueryRequest Create(string query) =>
            New().SetQuery(query).Create();

        public static QueryRequestBuilder New() => new();

        public static QueryRequestBuilder From(IQueryRequest request)
        {
            var builder = new QueryRequestBuilder
            {
                _query = request.Query,
                _queryName = request.QueryId,
                _queryHash = request.QueryHash,
                _operationName = request.OperationName,
                _readOnlyVariableValues = request.VariableValues,
                _initialValue = request.InitialValue,
                _readOnlyProperties = request.ContextData,
                _readOnlyExtensions = request.Extensions,
                _services = request.Services
            };

            if (builder._query is null && builder._queryName is null)
            {
                throw new QueryRequestBuilderException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNull);
            }

            return builder;
        }

        public static QueryRequestBuilder From(GraphQLRequest request)
        {
            QueryRequestBuilder builder = New();

            builder
                .SetQueryId(request.QueryId)
                .SetQueryHash(request.QueryHash)
                .SetOperation(request.OperationName)
                .SetVariableValues(request.Variables)
                .SetExtensions(request.Extensions);

            if (request.Query is not null)
            {
                builder.SetQuery(request.Query);
            }

            return builder;
        }
    }
}
