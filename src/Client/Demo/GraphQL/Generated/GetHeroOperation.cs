using System.Collections.Generic;
using StrawberryShake;

namespace Foo
{
    public class GetHeroOperation
        : IOperation<IGetHero>
    {
        private bool _fooSet;
        private ReviewInput _foo;

        public string Name => "GetHero";

        public IDocument Document => Queries.Default;

        public ReviewInput Foo
        {
            get => _foo;
            set
            {
                _foo = value;
                _fooSet = true;
            }
        }

        public IReadOnlyList<VariableValue> GetVariableValues()
        {
            var variables = new List<VariableValue>();

            if (_fooSet)
            {
                variables.Add(new VariableValue("foo", "ReviewInput", Foo));
            }

            return variables;
        }
    }
}
