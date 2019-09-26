using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        private bool _modified_episode;

        private Episode? _value_episode;

        public string Name => "getHero";

        public IDocument Document => Queries.Default;

        public Type ResultType => typeof(IGetHero);

        public Episode? Episode
        {
            get => _value_episode;
            set
            {
                _value_episode = value;
                _modified_episode = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(_modified_episode)
            {
                variables.Add(new VariableValue("episode", "Episode", Episode));
            }

            return variables;
        }
    }
}
