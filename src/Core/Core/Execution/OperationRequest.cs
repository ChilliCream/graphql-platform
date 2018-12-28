using System;
using System.Collections.Generic;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal class OperationRequest
    {
        public OperationRequest(IServiceProvider services)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
        }

        public IServiceProvider Services { get; }

        public IReadOnlyDictionary<string, object> VariableValues { get; set; }

        public IReadOnlyDictionary<string, object> Custom { get; set; }

        public object InitialValue { get; set; }
    }
}
