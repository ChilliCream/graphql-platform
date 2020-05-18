using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeDiscoverer
    {
        private readonly Dictionary<ITypeReference, RegisteredType> _registeredTypes =
            new Dictionary<ITypeReference, RegisteredType>();
        private readonly List<ITypeReference> _unregistered = new List<ITypeReference>();
        private readonly List<ISchemaError> _errors = new List<ISchemaError>();
        private readonly IDictionary<IClrTypeReference, ITypeReference> _clrTypeReferences;
        private readonly TypeRegistrar _typeRegistrar;
        private readonly ITypeRegistrarHandler[] _handlers;

        public TypeDiscoverer(
            ISet<ITypeReference> initialTypes,
            IDictionary<IClrTypeReference, ITypeReference> clrTypeReferences,
            IDescriptorContext descriptorContext,
            IDictionary<string, object> contextData,
            IServiceProvider services)
        {
            _unregistered.AddRange(IntrospectionTypes.All);
            _unregistered.AddRange(Directives.All);
            _unregistered.AddRange(clrTypeReferences.Values);
            _unregistered.AddRange(initialTypes);

            _clrTypeReferences = clrTypeReferences;

            _typeRegistrar = new TypeRegistrar(
                _registeredTypes,
                clrTypeReferences,
                descriptorContext,
                contextData,
                services);

            _handlers = new ITypeRegistrarHandler[]
            {
                new SchemaTypeReferenceHandler(),
                new ClrTypeReferenceHandler(),
                new SyntaxTypeReferenceHandler()
            };
        }

        public DiscoveredTypes DiscoverTypes()
        {
            const int max = 1000;
            int tries = 0;
            bool resolved = false;

            do
            {
                try
                {
                    tries++;
                    RegisterTypes();
                    resolved = TryInferTypes();
                }
                catch (SchemaException ex)
                {
                    _errors.AddRange(ex.Errors);
                    break;
                }
                catch (Exception ex)
                {
                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetException(ex)
                        .Build());
                    break;
                }
            }
            while (resolved && tries < max);

            CollectErrors();

            return new DiscoveredTypes(
                _registeredTypes,
                _clrTypeReferences,
                _errors);
        }

        private void RegisterTypes()
        {
            while (_unregistered.Count > 0)
            {
                for (int i = 0; i < _handlers.Length; i++)
                {
                    _handlers[i].Register(_typeRegistrar, _unregistered);
                }

                _unregistered.Clear();
                _unregistered.AddRange(_typeRegistrar.GetUnhandled());
            }
        }

        private bool TryInferTypes()
        {
            bool inferred = false;

            foreach (IClrTypeReference unresolvedType in _typeRegistrar.GetUnresolved())
            {
                if (Scalars.TryGetScalar(unresolvedType.Type, out IClrTypeReference schemaType))
                {
                    inferred = true;

                    _unregistered.Add(schemaType);
                    _typeRegistrar.MarkResolved(unresolvedType);

                    if (!_clrTypeReferences.ContainsKey(unresolvedType))
                    {
                        _clrTypeReferences.Add(unresolvedType, schemaType);
                    }
                }
                else if (SchemaTypeResolver.TryInferSchemaType(unresolvedType, out schemaType))
                {
                    inferred = true;

                    _unregistered.Add(schemaType);
                    _typeRegistrar.MarkResolved(unresolvedType);
                }
            }

            return inferred;
        }

        private void CollectErrors()
        {
            foreach (InitializationContext context in
                _registeredTypes.Values.Distinct().Select(t => t.InitializationContext))
            {
                _errors.AddRange(context.Errors);
            }

            IReadOnlyCollection<ITypeReference> unresolved = _typeRegistrar.GetUnresolved();

            if (_errors.Count == 0 && unresolved.Count > 0)
            {
                foreach (IClrTypeReference unresolvedReference in _typeRegistrar.GetUnresolved())
                {
                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(
                            TypeResources.TypeRegistrar_TypesInconsistent,
                            unresolvedReference)
                        .SetExtension(
                            TypeErrorFields.Reference,
                            unresolvedReference)
                        .SetCode(ErrorCodes.Schema.UnresolvedTypes)
                        .Build());
                }
            }
        }
    }
}
