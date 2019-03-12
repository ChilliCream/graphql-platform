using System;
using System.Collections.Generic;

namespace HotChocolate.Execution
{
    public interface IQueryRequestBuilder
    {
        IQueryRequestBuilder SetQuery(
            string query);
        IQueryRequestBuilder SetOperation(
            string operationName);
        IQueryRequestBuilder SetVariableValues(
            IDictionary<string, object> variableValues);
        IQueryRequestBuilder AddVariableValue(
            string name, object value);
        IQueryRequestBuilder SetVariableValue(
            string name, object value);
        IQueryRequestBuilder SetInitialValue(
            object initialValue);
        IQueryRequestBuilder SetProperties(
            IDictionary<string, object> properties);
        IQueryRequestBuilder AddProperty(
            string name, object value);
        IQueryRequestBuilder SetProperty(
            string name, object value);
        IQueryRequestBuilder SetServices(
            IServiceProvider services);
        IReadOnlyQueryRequest Create();
    }
}
