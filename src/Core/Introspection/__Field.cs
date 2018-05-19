using System.Collections.Generic;
using System.Linq;
using HotChocolate.Abstractions;

namespace HotChocolate.Introspection
{
    internal class __Field
    {
        private IType _type;

        internal __Field(string name, string description,
            IEnumerable<__InputValue> arguments,
            IType type, bool isDeprecated,
            string deprecationReason)
        {
            Name = name;
            Description = description;
            Arguments = arguments.ToArray();
            _type = type;
            IsDeprecated = isDeprecated;
            DeprecationReason = deprecationReason;
        }

        public string Name { get; }
        public string Description { get; }

        [GraphQLName("args")]
        public IReadOnlyCollection<__InputValue> Arguments { get; }

        [GraphQLName("type")]
        public __Type GetType(ISchema schema)
        {
            return __Type.CreateType(schema, _type);
        }

        public bool IsDeprecated { get; }

        public string DeprecationReason { get; }
    }
}