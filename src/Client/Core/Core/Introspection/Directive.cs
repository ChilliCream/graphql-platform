using System;
using System.Linq.Expressions;
using HotChocolate.Client.Core.Builders;

namespace HotChocolate.Client.Core.Introspection
{
    public class Directive : QueryableValue<Directive>
    {
        public Directive(Expression expression)
            : base(expression)
        {
        }

        public string Name { get; }
        public string Description { get; }
        public DirectiveLocation Locations { get; }
        public IQueryableList<InputValue> Args => this.CreateProperty(x => x.Args);
    }
}
