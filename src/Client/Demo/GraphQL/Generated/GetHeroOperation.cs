using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        private bool _isSet_foo;

        private ReviewInput _foo;

        public string Name => "getHero";

        public IDocument Document => Queries.Default;

        public ReviewInput Foo
        {
            get => _foo;
            set
            {
                _foo = value;
                _isSet_foo = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if(_isSet_foo)
            {
                variables.Add(new VariableValue("foo", "ReviewInput", Foo));
            }

            return variables;
        }
    }
}
