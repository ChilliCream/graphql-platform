using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client.GraphQL
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        public string Name => "getHero";

        public IDocument Document => Queries.Default;

        public OperationKind Kind => OperationKind.Query;

        public Type ResultType => typeof(IGetHero);

        public Optional<Episode?> Episode { get; set; }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (Episode.HasValue)
            {
                variables.Add(new VariableValue("episode", "Episode", Episode.Value));
            }

            return variables;
        }
    }
}
