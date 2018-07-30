using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal class OperationRequest
    {
        public OperationRequest(
            IServiceProvider services)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceProvider Services { get; }
        public DataLoaderState DataLoaders { get; set; }
        public IReadOnlyDictionary<string, IValueNode> VariableValues { get; set; }
        public object InitialValue { get; set; }
    }
}
