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
        private readonly Dictionary<Type, INamedType> _dotnetTypeToSchemaType =
            new Dictionary<Type, INamedType>();
        private readonly Dictionary<Type, List<INamedType>> _nativeTypes =
            new Dictionary<Type, List<INamedType>>();
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
                GetTypes().OfType<INamedInputType>())
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
            if (_nativeTypes.TryGetValue(type, out List<INamedType> types)
                && !types.Contains(namedType))
            {
                types.Add(namedType);
            }
        }

    }
}
