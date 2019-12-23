using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class GetUserOperation
        : IOperation<IGetUser>
    {
        public string Name => "getUser";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Query;

        public Type ResultType => typeof(IGetUser);

        public Optional<string> Login { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (Login.HasValue)
            {
                variables.Add(new VariableValue("login", "String", Login.Value));
            }

            return variables;
        }
    }
}
