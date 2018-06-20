using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal partial class TypeRegistry
        : ITypeRegistry
    {
        private readonly object _sync = new object();
        private readonly TypeInspector _typeInspector = new TypeInspector();
        private readonly Dictionary<string, INamedType> _namedTypes =
            new Dictionary<string, INamedType>();
        private readonly Dictionary<string, ITypeBinding> _typeBindings =
            new Dictionary<string, ITypeBinding>();
        private readonly Dictionary<Type, string> _dotnetTypeToSchemaType =
            new Dictionary<Type, string>();
        private readonly Dictionary<Type, HashSet<string>> _nativeTypes =
            new Dictionary<Type, HashSet<string>>();
        private readonly IServiceProvider _serviceProvider;
        private bool _sealed;

        public TypeRegistry(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }

        public void CompleteRegistartion()
        {
            lock (_sync)
            {
                if (!_sealed)
                {
                    CreateNativeTypeLookup();
                    _sealed = true;
                }
            }
        }

        private void CreateNativeTypeLookup()
        {
            AddNativeTypeBindingFromInputTypes();
            AddNativeTypeBindingFromTypeBindings();
        }

        private void AddNativeTypeBindingFromInputTypes()
        {
            foreach (INamedInputType inputType in
                GetTypes().OfType<INamedInputType>()
                .Where(t => t.NativeType != null))
            {
                AddNativeTypeBinding(inputType.NativeType, inputType);
            }
        }

        private void AddNativeTypeBindingFromTypeBindings()
        {
            foreach (ITypeBinding typeBinding in
                _typeBindings.OfType<ITypeBinding>())
            {
                if (typeBinding.Type != null && _namedTypes.TryGetValue(
                    typeBinding.Name, out INamedType namedType))
                {
                    AddNativeTypeBinding(typeBinding.Type, namedType);
                }
            }
        }

        private void AddNativeTypeBinding(Type type, INamedType namedType)
        {
            if (_nativeTypes.TryGetValue(type, out HashSet<string> types))
            {
                types.Add(namedType.Name);
            }
        }

    }
}
