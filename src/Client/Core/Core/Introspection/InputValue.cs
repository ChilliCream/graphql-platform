using System;
using System.Linq.Expressions;
using HotChocolate.Client.Core.Builders;

namespace HotChocolate.Client.Core.Introspection
{
    public class InputValue : QueryableValue<InputValue>
    {
        public InputValue(Expression expression)
            : base(expression)
        {
        }

        public string Name { get; }
        public string Description { get; }
        public SchemaType Type => this.CreateProperty(x => x.Type, SchemaType.Create);
        public string DefaultValue { get; }
    }
}
