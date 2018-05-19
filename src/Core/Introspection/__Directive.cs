using System.Collections.Generic;

namespace HotChocolate.Introspection
{
    internal class __Directive
    {
        public string Name { get; }
        public string Description { get; }
        public __DirectiveLocation Location { get; }
        public IReadOnlyCollection<__InputValue> Arguments { get; }
    }
}