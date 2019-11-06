using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class GetCharacterOperation
        : IOperation<IGetCharacter>
    {
        public string Name => "getCharacter";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(IGetCharacter);

        public Optional<IReadOnlyList<string?>> Ids { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(Ids.HasValue)
            {
                variables.Add(new VariableValue("ids", "String", Ids.Value));
            }

            return variables;
        }
    }
}
