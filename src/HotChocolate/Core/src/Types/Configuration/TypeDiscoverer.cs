using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Introspection;

#nullable enable

namespace HotChocolate.Configuration
{
    internal sealed class TypeDiscoverer
    {
        private readonly List<ITypeReference> _unregistered = new();
        private readonly List<ISchemaError> _errors = new();
        private readonly TypeRegistry _typeRegistry;
        private readonly TypeRegistrar _typeRegistrar;
        private readonly ITypeRegistrarHandler[] _handlers;
        private readonly ITypeInspector _typeInspector;

        public TypeDiscoverer(
            IDescriptorContext context,
            TypeRegistry typeRegistry,
            TypeLookup typeLookup,
            IEnumerable<ITypeReference> initialTypes,
            ITypeInterceptor interceptor,
            bool includeSystemTypes = true)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (typeRegistry is null)
            {
                throw new ArgumentNullException(nameof(typeRegistry));
            }

            if (typeLookup is null)
            {
                throw new ArgumentNullException(nameof(typeLookup));
            }

            if (initialTypes is null)
            {
                throw new ArgumentNullException(nameof(initialTypes));
            }

            if (interceptor is null)
            {
                throw new ArgumentNullException(nameof(interceptor));
            }

            _typeRegistry = typeRegistry;

            if (includeSystemTypes)
            {
                _unregistered.AddRange(
                    IntrospectionTypes.CreateReferences(context.TypeInspector));
                _unregistered.AddRange(
                    Directives.CreateReferences(context.TypeInspector));
            }

            _unregistered.AddRange(typeRegistry.GetTypeRefs());
            _unregistered.AddRange(initialTypes.Distinct());

            _typeRegistrar = new TypeRegistrar(context, typeRegistry, typeLookup, interceptor);

            _handlers = new ITypeRegistrarHandler[]
            {
                new SchemaTypeReferenceHandler(),
                new ExtendedTypeReferenceHandler(context.TypeInspector),
                new SyntaxTypeReferenceHandler(context.TypeInspector)
            };

            _typeInspector = context.TypeInspector;
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

            if (_errors.Count == 0)
            {
                _typeRegistry.CompleteDiscovery();
            }

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
                    _typeRegistry.TryRegister(unresolvedType, typeReference);
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
                _typeRegistry.Types.Select(t => t.DiscoveryContext))
            {
                _errors.AddRange(context.Errors);
            }

            IReadOnlyCollection<ITypeReference> unresolved = _typeRegistrar.GetUnresolved();

            if (_errors.Count == 0 && unresolved.Count > 0)
            {
                foreach (ITypeReference unresolvedReference in _typeRegistrar.GetUnresolved())
                {
                    var types = _typeRegistry.Types.Where(
                        t => t.Dependencies.Select(d => d.TypeReference)
                            .Any(r => r.Equals(unresolvedReference))).ToList();

                    ISchemaErrorBuilder builder =
                         SchemaErrorBuilder.New()
                            .SetMessage(
                                TypeResources.TypeRegistrar_TypesInconsistent,
                                unresolvedReference)
                            .SetExtension(

                                TypeErrorFields.Reference,
                                unresolvedReference)
                            .SetCode(ErrorCodes.Schema.UnresolvedTypes);

                    if (types.Count == 1)
                    {
                        builder.SetTypeSystemObject(types[0].Type);
                    }
                    else if (types.Count > 1)
                    {
                        builder
                            .SetTypeSystemObject(types[0].Type)
                            .SetExtension("involvedTypes", types.Select(t => t.Type).ToList());
                    }

                    _errors.Add(builder.Build());
                }
            }
        }
    }
}
