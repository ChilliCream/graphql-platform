using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface IRemoteQueryRequestBuilder
    {
        IRemoteQueryRequestBuilder SetQuery(
            DocumentNode query);
        IRemoteQueryRequestBuilder SetOperation(
            string operationName);
        IRemoteQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues);
        IRemoteQueryRequestBuilder AddVariableValue(
            string name, object value);
        IRemoteQueryRequestBuilder SetInitialValue(
            object initialValue);
        IRemoteQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties);
        IRemoteQueryRequestBuilder AddProperty(
            string name, object value);
        IRemoteQueryRequestBuilder SetServices(
            IServiceProvider services);
        IRemoteQueryRequest Create();
    }
}
