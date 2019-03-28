using System;
using HotChocolate.Configuration.Bindings;
using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration
{
    internal class BindingCompiler
        : IBindingCompiler
    {
        private static HashSet<Type> _supportedBindings = new HashSet<Type>
        {
            typeof(ComplexTypeBindingInfo),
            typeof(ResolverBindingInfo),
            typeof(ResolverTypeBindingInfo),
        };

        private List<IBindingInfo> _bindings = new List<IBindingInfo>();

        public bool CanHandle(IBindingInfo binding)
        {
            return binding != null
                && _supportedBindings.Contains(binding.GetType());
        }

        public void AddBinding(IBindingInfo binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            if (!CanHandle(binding))
            {
                throw new ArgumentException(
                    "The specified binding cannot be handled.",
                    nameof(binding));
            }

            _bindings.Add(binding);
        }

        public IBindingLookup Compile(
            IDescriptorContext descriptorContext)
        {
            throw new NotImplementedException();
        }
    }
}
