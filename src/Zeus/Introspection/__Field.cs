using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal class __Field
    {
        private __Field(string name, string description, IEnumerable<__InputValue> arguments, 
            IType type, bool isDepricated, string depricationReason)
        {
            Name = name;
            Description = description;
            Arguments = arguments.ToArray();
            Type = type;
            IsDepricated = isDepricated;
            DepricationReason = depricationReason;
        }

        public string Name { get; }
        public string Description { get; }

        [GraphQLName("args")]
        public IReadOnlyCollection<__InputValue> Arguments { get; }

        public IType Type { get; }

        public bool IsDepricated { get; }

        public string DepricationReason { get; }
    }
}