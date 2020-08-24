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
        private readonly IDictionary<ClrTypeReference, ITypeReference> _clrTypeReferences;
        private readonly TypeRegistrar _typeRegistrar;
        private readonly ITypeRegistrarHandler[] _handlers;

        public TypeDiscoverer(
            ISet<ITypeReference> initialTypes,
            IDictionary<ClrTypeReference, ITypeReference> clrTypeReferences,
            IDescriptorContext descriptorContext,
            ITypeInterceptor interceptor,
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
                interceptor,
                services);

            _handlers = new ITypeRegistrarHandler[]
            {
                new SchemaTypeReferenceHandler(),
                new ExtendedTypeReferenceHandler(descriptorContext.Inspector),
                new SyntaxTypeReferenceHandler()
            };
        }

        public DiscoveredTypes DiscoverTypes()
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
            var inferred = false;

            foreach (ClrTypeReference unresolvedType in
                _typeRegistrar.GetUnresolved().OfType<ClrTypeReference>())
            {
                if (Scalars.TryGetScalar(unresolvedType.Type.Type, out ClrTypeReference schemaType))
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
