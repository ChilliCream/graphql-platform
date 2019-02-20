using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching
{
    public partial class RemoteQueryRequestBuilder
        : IRemoteQueryRequestBuilder
    {
        private DocumentNode _query;
        private string _operationName;
        private IDictionary<string, object> _variableValues;
        private object _initialValue;
        private IDictionary<string, object> _properties;
        private IServiceProvider _services;

        public IRemoteQueryRequestBuilder SetQuery(DocumentNode query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            _query = query;
            return this;
        }

        public IRemoteQueryRequestBuilder SetOperation(string operationName)
        {
            _operationName = operationName;
            return this;
        }

        public IRemoteQueryRequestBuilder SetInitialValue(object initialValue)
        {
            _initialValue = initialValue;
            return this;
        }

        public IRemoteQueryRequestBuilder SetServices(
            IServiceProvider services)
        {
            _services = services;
            return this;
        }

        public IRemoteQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues)
        {
            _variableValues = variableValues;
            return this;
        }

        public IRemoteQueryRequestBuilder AddVariableValue(
            string name, object value)
        {
            if (_variableValues == null)
            {
                _variableValues = new Dictionary<string, object>();
            }

            _variableValues.Add(name, value);
            return this;
        }

        public IRemoteQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties)
        {
            _properties = properties;
            return this;
        }

        public IRemoteQueryRequestBuilder AddProperty(
            string name, object value)
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, object>();
            }

            _properties.Add(name, value);
            return this;
        }

        public IRemoteQueryRequest Create()
        {
            if (_query == null)
            {
                throw new QueryRequestBuilderException(
                    StitchingResources.QueryRequestBuilder_QueryIsNull);
            }

            ValidateOperation(_query, _operationName);

            return new RemoteQueryRequest
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

        private static void ValidateOperation(
            DocumentNode query,
            string operationName)
        {
            var operations = query.Definitions
                .OfType<OperationDefinitionNode>()
                .ToList();

            if (string.IsNullOrEmpty(operationName))
            {
                if (operations.Count == 1)
                {
                    return;
                }

                throw new QueryRequestBuilderException(StitchingResources
                    .QueryRequestBuilder_OperationNameMissing);
            }
            else
            {
                OperationDefinitionNode operation =
                    operations.SingleOrDefault(t =>
                        t.Name.Value.Equals(operationName,
                            StringComparison.Ordinal));
                if (operation == null)
                {
                    throw new QueryRequestBuilderException(
                        string.Format(CultureInfo.InvariantCulture,
                            StitchingResources
                                .QueryRequestBuilder_OperationNameMissing,
                            operationName));
                }
            }
        }

        public static RemoteQueryRequestBuilder New() =>
            new RemoteQueryRequestBuilder();
    }
}
