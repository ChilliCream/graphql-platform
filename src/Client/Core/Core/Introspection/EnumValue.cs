using System;
using System.Linq.Expressions;

namespace HotChocolate.Client.Core.Introspection
{
    public class EnumValue : QueryableValue<EnumValue>
    {
        public EnumValue(Expression expression)
            : base(expression)
        {
        }

        public string Name { get; }
        public string Description { get; }
        public bool IsDeprecated { get; }
        public string DeprecationReason { get; }

        internal static EnumValue Create(Expression expression)
        {
            return new EnumValue(expression);
        }
    }
}
