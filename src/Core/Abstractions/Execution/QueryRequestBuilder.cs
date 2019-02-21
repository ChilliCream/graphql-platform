using System;
using System.Collections.Generic;
using HotChocolate.Properties;

namespace HotChocolate.Execution
{
    public partial class QueryRequestBuilder
        : IQueryRequestBuilder
    {
        private string _query;
        private string _operationName;
        private IDictionary<string, object> _variableValues;
        private object _initialValue;
        private IDictionary<string, object> _properties;
        private IServiceProvider _services;

        public IQueryRequestBuilder SetQuery(string query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
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

        public IQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues)
        {
            _variableValues = variableValues;
            return this;
        }

        public IQueryRequestBuilder AddVariableValue(
            string name, object value)
        {
            if (_variableValues == null)
            {
                _variableValues = new Dictionary<string, object>();
            }

            _variableValues.Add(name, value);
            return this;
        }

        public IQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties)
        {
            _properties = properties;
            return this;
        }

        public IQueryRequestBuilder AddProperty(
            string name, object value)
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, object>();
            }

            _properties.Add(name, value);
            return this;
        }

        public IReadOnlyQueryRequest Create()
        {
            if (_query == null)
            {
                throw new QueryRequestBuilderException(
                    AbstractionResources.QueryRequestBuilder_QueryIsNull);
            }

            return new QueryRequest
            {
                Query = _query,
                OperationName = _operationName,
                InitialValue = _initialValue,
                Services = _services,
                VariableValues = _variableValues == null
                    ? null
                    : new Dictionary<string, object>(_variableValues),
                Properties = _properties == null
                    ? null
                    : new Dictionary<string, object>(_properties)
            };
        }

        public static QueryRequestBuilder New() =>
            new QueryRequestBuilder();
    }
}
