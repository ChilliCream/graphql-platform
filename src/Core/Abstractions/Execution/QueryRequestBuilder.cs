using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    public partial class QueryRequestBuilder
        : IQueryRequestBuilder
    {
        private readonly static ExtensionData _empty = new ExtensionData();
        private IQuery _query;
        private string _queryName;
        private string _queryHash;
        private string _operationName;
        private IReadOnlyDictionary<string, object> _readOnlyVariableValues;
        private Dictionary<string, object> _variableValues;
        private object _initialValue;
        private IReadOnlyDictionary<string, object> _readOnlyProperties;
        private Dictionary<string, object> _properties;
        private IReadOnlyDictionary<string, object> _readOnlyExtensions;
        private Dictionary<string, object> _extensions;
        private IServiceProvider _services;

        public IQueryRequestBuilder SetQuery(string querySource)
        {
            if (string.IsNullOrEmpty(querySource))
            {
                throw new ArgumentException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNullOrEmpty,
                    nameof(querySource));
            }

            _query = new QuerySourceText(querySource);
            return this;
        }

        public IQueryRequestBuilder SetQuery(DocumentNode queryDocument)
        {
            if (queryDocument is null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            _query = new QueryDocument(queryDocument);
            return this;
        }

        public IQueryRequestBuilder SetQueryName(string queryName)
        {
            _queryName = queryName;
            return this;
        }

        public IQueryRequestBuilder SetQueryHash(string queryHash)
        {
            _queryHash = queryHash;
            return this;
        }

        public IQueryRequestBuilder SetOperation(string operationName)
        {
            _operationName = operationName;
            return this;
        }

        public IQueryRequestBuilder SetInitialValue(object initialValue)
        {
            _initialValue = initialValue;
            return this;
        }

        public IQueryRequestBuilder SetServices(
            IServiceProvider services)
        {
            _services = services;
            return this;
        }

        public IQueryRequestBuilder TrySetServices(
            IServiceProvider services)
        {
            if (_services is null)
            {
                _services = services;
            }
            return this;
        }

        public IQueryRequestBuilder SetVariableValues(
            Dictionary<string, object> variableValues) =>
            SetVariableValues((IDictionary<string, object>)variableValues);


        public IQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues)
        {
            _variableValues = variableValues is null
                ? null
                : new Dictionary<string, object>(variableValues);
            _readOnlyVariableValues = null;
            return this;
        }

        public IQueryRequestBuilder SetVariableValues(
           IReadOnlyDictionary<string, object> variableValues)
        {
            _variableValues = null;
            _readOnlyVariableValues = variableValues;
            return this;
        }

        public IQueryRequestBuilder SetVariableValue(string name, object value)
        {
            InitializeVariables();

            _variableValues[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddVariableValue(
            string name, object value)
        {
            InitializeVariables();

            _variableValues.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddVariableValue(
            string name, object value)
        {
            InitializeVariables();

            if (!_variableValues.ContainsKey(name))
            {
                _variableValues.Add(name, value);
            }
            return this;
        }

        public IQueryRequestBuilder SetProperties(
            Dictionary<string, object> properties) =>
            SetProperties((IDictionary<string, object>)properties);


        public IQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties)
        {
            _properties = properties is null
                ? null
                : new Dictionary<string, object>(properties);
            _readOnlyProperties = null;
            return this;
        }

        public IQueryRequestBuilder SetProperties(
            IReadOnlyDictionary<string, object> properties)
        {
            _properties = null;
            _readOnlyProperties = properties;
            return this;
        }

        public IQueryRequestBuilder SetProperty(string name, object value)
        {
            InitializeProperties();

            _properties[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddProperty(
            string name, object value)
        {
            InitializeProperties();

            _properties.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddProperty(
            string name, object value)
        {
            InitializeProperties();

            if (!_properties.ContainsKey(name))
            {
                _properties.Add(name, value);
            }
            return this;
        }

        public IQueryRequestBuilder SetExtensions(
            Dictionary<string, object> extensions) =>
            SetExtensions((IDictionary<string, object>)extensions);

        public IQueryRequestBuilder SetExtensions(
            IDictionary<string, object> extensions)
        {
            _extensions = extensions is null
                ? null
                : new Dictionary<string, object>(extensions);
            _readOnlyExtensions = null;
            return this;
        }

        public IQueryRequestBuilder SetExtensions(
            IReadOnlyDictionary<string, object> extensions)
        {
            _extensions = null;
            _readOnlyExtensions = extensions;
            return this;
        }

        public IQueryRequestBuilder SetExtension(string name, object value)
        {
            InitializeExtensions();

            _extensions[name] = value;
            return this;
        }

        public IQueryRequestBuilder AddExtension(
            string name, object value)
        {
            InitializeExtensions();

            _extensions.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder TryAddExtension(
            string name, object value)
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
            if (_query is null && _queryName is null)
            {
                throw new QueryRequestBuilderException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNull);
            }

            return new QueryRequest
            {
                Query = _query,
                QueryName = _queryName,
                QueryHash = _queryHash,
                OperationName = _operationName,
                InitialValue = _initialValue,
                Services = _services,
                VariableValues = GetVariableValues(),
                Properties = GetProperties(),
                Extensions = GetExtensions()
            };
        }

        private IReadOnlyDictionary<string, object> GetVariableValues()
        {
            if (_variableValues is { })
            {
                return _variableValues;
            }
            return _readOnlyVariableValues ?? _empty;
        }

        private void InitializeVariables()
        {
            if (_variableValues == null)
            {
                _variableValues = _readOnlyVariableValues == null
                    ? new Dictionary<string, object>()
                    : _readOnlyVariableValues.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyVariableValues = null;
            }
        }

        private IReadOnlyDictionary<string, object> GetProperties()
        {
            if (_properties is { })
            {
                return _properties;
            }
            return _readOnlyProperties ?? _empty;
        }

        private void InitializeProperties()
        {
            if (_properties == null)
            {
                _properties = _readOnlyProperties == null
                    ? new Dictionary<string, object>()
                    : _readOnlyProperties.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyProperties = null;
            }
        }

        private IReadOnlyDictionary<string, object> GetExtensions()
        {
            if (_extensions != null)
            {
                return _extensions;
            }
            return _readOnlyExtensions ?? _empty;
        }

        private void InitializeExtensions()
        {
            if (_extensions == null)
            {
                _extensions = _readOnlyExtensions == null
                    ? new Dictionary<string, object>()
                    : _readOnlyExtensions.ToDictionary(
                        t => t.Key, t => t.Value);
                _readOnlyExtensions = null;
            }
        }

        public static IReadOnlyQueryRequest Create(string query) =>
            QueryRequestBuilder.New().SetQuery(query).Create();

        public static QueryRequestBuilder New() =>
            new QueryRequestBuilder();

        public static QueryRequestBuilder From(IReadOnlyQueryRequest request)
        {
            var builder = new QueryRequestBuilder();
            builder._query = request.Query;
            builder._queryName = request.QueryName;
            builder._queryHash = request.QueryHash;
            builder._operationName = request.OperationName;
            builder._readOnlyVariableValues = request.VariableValues;
            builder._initialValue = request.InitialValue;
            builder._readOnlyProperties = request.Properties;
            builder._readOnlyExtensions = request.Extensions;
            builder._services = request.Services;

            if (builder._query is null && builder._queryName is null)
            {
                throw new QueryRequestBuilderException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNull);
            }

            return builder;
        }

        public static QueryRequestBuilder From(GraphQLRequest request)
        {
            var builder = QueryRequestBuilder.New();

            builder
                .SetQueryName(request.QueryName)
                .SetQueryHash(request.QueryHash)
                .SetOperation(request.OperationName)
                .SetVariableValues(request.Variables)
                .SetExtensions(request.Extensions);

            if (request.Query != null)
            {
                builder.SetQuery(request.Query);
            }

            return builder;
        }
    }
}
