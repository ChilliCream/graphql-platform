using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class BindingLookup
        : IBindingLookup
    {
        private readonly IDescriptorContext _context;

        private readonly Dictionary<NameString, TypeBindingInfo> _bindings;

        public BindingLookup(
            IDescriptorContext context,
            IEnumerable<TypeBindingInfo> bindings)
        {
            if (bindings is null)
            {
                throw new ArgumentNullException(nameof(bindings));
            }

            _context = context
                ?? throw new ArgumentNullException(nameof(context));
            _bindings = bindings.ToDictionary(t => t.Name);
        }

        public IReadOnlyCollection<ITypeBindingInfo> Bindings =>
            _bindings.Values;

        public ITypeBindingInfo GetBindingInfo(NameString typeName)
        {
            if (!_bindings.TryGetValue(typeName, out TypeBindingInfo binding))
            {
                binding = new TypeBindingInfo(
                    _context,
                    typeName,
                    null,
                    BindingBehavior.Explicit,
                    new Dictionary<NameString, RegisteredResolver>(),
                    new Dictionary<NameString, MemberInfo>());
                _bindings.Add(typeName, binding);
            }
            return binding;
        }
    }
}
