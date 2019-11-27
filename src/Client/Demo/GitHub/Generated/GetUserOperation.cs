using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public class GetUserOperation
        : IOperation<IGetUser>
    {
        public string Name => "getUser";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(IGetUser);

        public OperationKind Kind => OperationKind.Query;

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
