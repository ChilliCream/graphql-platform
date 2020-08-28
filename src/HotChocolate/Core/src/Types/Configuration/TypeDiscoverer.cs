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
        private readonly List<ITypeReference> _unregistered = new List<ITypeReference>();
        private readonly List<ISchemaError> _errors = new List<ISchemaError>();
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeRegistrar _typeRegistrar;
        private readonly ITypeRegistrarHandler[] _handlers;
        private readonly ITypeInspector _typeInspector;

        public TypeDiscoverer(
            TypeRegistry typeRegistry,
            ISet<ITypeReference> initialTypes,
            IDescriptorContext descriptorContext,
            ITypeInterceptor interceptor,
            IServiceProvider services,
            bool includeSystemTypes = true)
        {
            _typeRegistry = typeRegistry;

            if (includeSystemTypes)
            {
                _unregistered.AddRange(
                    IntrospectionTypes.CreateReferences(descriptorContext.TypeInspector));
                _unregistered.AddRange(
                    Directives.CreateReferences(descriptorContext.TypeInspector));
            }

            _unregistered.AddRange(typeRegistry.GetTypeRefs());
            _unregistered.AddRange(initialTypes);

            _typeRegistrar = new TypeRegistrar(
                _typeRegistry,
                descriptorContext,
                interceptor,
                services);

            _handlers = new ITypeRegistrarHandler[]
            {
                new SchemaTypeReferenceHandler(),
                new ExtendedTypeReferenceHandler(descriptorContext.TypeInspector),
                new SyntaxTypeReferenceHandler(descriptorContext.TypeInspector)
            };

            _typeInspector = descriptorContext.TypeInspector;
        }

        public IReadOnlyList<ISchemaError> DiscoverTypes()
        {
            const int max = 1000;
            var tries = 0;
            var resolved = false;

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
                }
                catch (Exception ex)
                {
                    _errors.Add(SchemaErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetException(ex)
                        .Build());
                }
            }
            while (resolved && tries < max && _errors.Count == 0);

            CollectErrors();

            return _errors;
        }

        private void RegisterTypes()
        {
            while (_unregistered.Count > 0)
            {
                for (var i = 0; i < _handlers.Length; i++)
                {
                    _handlers[i].Register(_typeRegistrar, _unregistered);
                }

                _unregistered.Clear();
                _unregistered.AddRange(_typeRegistrar.GetUnhandled());
            }
        }

        private bool TryInferTypes()
        {
            var inferred = false;

            foreach (ExtendedTypeReference unresolvedType in
                _typeRegistrar.GetUnresolved().OfType<ExtendedTypeReference>())
            {
                if (Scalars.TryGetScalar(unresolvedType.Type.Type, out Type? scalarType))
                {
                    inferred = true;

                    ExtendedTypeReference typeReference = _typeInspector.GetTypeRef(scalarType);
                    _unregistered.Add(typeReference);
                    _typeRegistrar.MarkResolved(unresolvedType);

                    if (!_clrTypeReferences.ContainsKey(unresolvedType))
                    {
                        _clrTypeReferences.Add(unresolvedType, typeReference);
                    }
                }
                else if (SchemaTypeResolver.TryInferSchemaType(
                    _typeInspector, unresolvedType, out ExtendedTypeReference schemaType))
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
            foreach (TypeDiscoveryContext context in
                _registeredTypes.Values.Distinct().Select(t => t.DiscoveryContext))
            {
                _errors.AddRange(context.Errors);
            }

            IReadOnlyCollection<ITypeReference> unresolved = _typeRegistrar.GetUnresolved();

            if (_errors.Count == 0 && unresolved.Count > 0)
            {
                foreach (ITypeReference unresolvedReference in _typeRegistrar.GetUnresolved())
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
