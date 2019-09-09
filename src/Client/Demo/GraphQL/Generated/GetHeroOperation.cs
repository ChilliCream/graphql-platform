using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        public string Name => "getHero";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(IGetHero);

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            return Array.Empty<VariableValue>();
        }
    }
}
