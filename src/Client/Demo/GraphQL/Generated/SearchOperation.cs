using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class SearchOperation
        : IOperation<ISearch>
    {
        public string Name => "search";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Query;

        public Type ResultType => typeof(ISearch);

        public Optional<string> Text { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (Text.HasValue)
            {
                variables.Add(new VariableValue("text", "String", Text.Value));
            }

            return variables;
        }
    }
}
