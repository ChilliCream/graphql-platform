using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public partial class OnPublishDocumentOperation
        : IOperation<IOnPublishDocument>
    {
        public string Name => "onPublishDocument";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Subscription;

        public Type ResultType => typeof(IOnPublishDocument);

        public Optional<string> SessionId { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (SessionId.HasValue)
            {
                variables.Add(new VariableValue("sessionId", "String", SessionId.Value));
            }

            return variables;
        }
    }
}
