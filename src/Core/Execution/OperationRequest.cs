using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    public class OperationRequest
    {
        public OperationRequest(
            IServiceProvider services,
            IDataLoaderState dataLoaders)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
            DataLoaders = dataLoaders
                ?? throw new ArgumentNullException(nameof(dataLoaders));
        }

        public IServiceProvider Services { get; }
        public IDataLoaderState DataLoaders { get; }
        public IReadOnlyDictionary<string, IValueNode> VariableValues { get; set; }
        public object InitialValue { get; set; }
    }
}
