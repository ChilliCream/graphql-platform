using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Runtime;

namespace HotChocolate.Execution
{
    internal class OperationRequest
    {
        public OperationRequest(
            IServiceProvider services,
            ISession session)
        {
            Services = services
                ?? throw new ArgumentNullException(nameof(services));
            Session = session
                ?? throw new ArgumentNullException(nameof(session));
        }

        public IServiceProvider Services { get; }
        public ISession Session { get; private set; }
        public IReadOnlyDictionary<string, object> VariableValues { get; set; }
        public IReadOnlyDictionary<string, object> Properties { get; set; }
        public object InitialValue { get; set; }

        public OperationRequest Clone(ISession session)
        {
            var clonedRequest = (OperationRequest)MemberwiseClone();
            clonedRequest.Session = session
                ?? throw new ArgumentNullException(nameof(session));
            return clonedRequest;
        }
    }
}
