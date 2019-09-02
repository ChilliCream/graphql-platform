using System;
using System.Collections;
using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        private bool _modified_foo;

        private ReviewInput _value_foo;

        public string Name => "getHero";

        public IDocument Document => Queries.Default;

        public ReviewInput Foo
        {
            get => _value_foo;
            set
            {
                _value_foo = value;
                _modified_foo = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (_modified_foo)
            {
                variables.Add(new VariableValue("foo", "ReviewInput", Foo));
            }

            return variables;
        }
    }
}
