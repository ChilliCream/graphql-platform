using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class AggregateTypeInterceptor : TypeInterceptor
{
    private readonly List<ITypeDiscoveryContext> _discoveryContexts = new();
    private readonly List<ITypeCompletionContext> _completionContexts = new();
    private readonly List<ITypeReference> _typeReferences = new();
    private IReadOnlyCollection<TypeInterceptor> _typeInterceptors;
    private IReadOnlyCollection<ITypeInitializationInterceptor> _initInterceptors;
    private IReadOnlyCollection<ITypeInitializationInterceptor> _agrInterceptors;
    private IReadOnlyCollection<ITypeScopeInterceptor> _scopeInterceptors;
    private IReadOnlyCollection<ITypeInitializationFlowInterceptor> _flowInterceptors;
    private IReadOnlyCollection<ITypeRegistryInterceptor> _registryInterceptors;
    private bool _triggerAggregations;

    public AggregateTypeInterceptor()
    {
        _typeInterceptors = Array.Empty<TypeInterceptor>();
        _initInterceptors = Array.Empty<ITypeInitializationInterceptor>();
        _agrInterceptors = Array.Empty<ITypeInitializationInterceptor>();
        _scopeInterceptors = Array.Empty<ITypeScopeInterceptor>();
        _flowInterceptors = Array.Empty<ITypeInitializationFlowInterceptor>();
        _registryInterceptors = Array.Empty<ITypeRegistryInterceptor>();
        _triggerAggregations = false;
    }

    public void SetInterceptors(IReadOnlyCollection<object> interceptors)
    {
        _discoveryContexts.Clear();
        _completionContexts.Clear();
        _typeReferences.Clear();

        _typeInterceptors = interceptors.OfType<TypeInterceptor>().ToList();
        _initInterceptors = interceptors.OfType<ITypeInitializationInterceptor>().ToList();
        _agrInterceptors = _initInterceptors.Where(t => t.TriggerAggregations).ToList();
        _scopeInterceptors = interceptors.OfType<ITypeScopeInterceptor>().ToList();
        _flowInterceptors = interceptors.OfType<ITypeInitializationFlowInterceptor>().ToList();
        _registryInterceptors = interceptors.OfType<ITypeRegistryInterceptor>().ToList();
        _triggerAggregations = _agrInterceptors.Count > 0;
    }

    public override bool TriggerAggregations => _triggerAggregations;

    public override bool CanHandle(ITypeSystemObjectContext context) => true;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        foreach (var interceptor in _typeInterceptors)
        {
            interceptor.InitializeContext(
                context,
                typeInitializer,
                typeRegistry,
                typeLookup,
                typeReferenceResolver);
        }
    }

    public override void OnBeforeDiscoverTypes()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnBeforeDiscoverTypes();
        }
    }

    public override void OnAfterDiscoverTypes()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnAfterDiscoverTypes();
        }
    }

    public override void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(discoveryContext))
            {
                interceptor.OnBeforeInitialize(discoveryContext);
            }
        }
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(discoveryContext))
            {
                interceptor.OnAfterInitialize(discoveryContext, definition, contextData);
            }
        }
    }

    public override void OnTypeRegistered(ITypeDiscoveryContext discoveryContext)
    {
        foreach (var interceptor in _registryInterceptors)
        {
            interceptor.OnTypeRegistered(discoveryContext);
        }
    }

    public override IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        _typeReferences.Clear();

        if (_agrInterceptors.Count > 0)
        {
            foreach (var interceptor in _agrInterceptors)
            {
                _discoveryContexts.Clear();

                foreach (var discoveryContext in discoveryContexts)
                {
                    if (interceptor.CanHandle(discoveryContext))
                    {
                        _discoveryContexts.Add(discoveryContext);
                    }
                }

                _typeReferences.AddRange(
                    interceptor.RegisterMoreTypes(_discoveryContexts).Distinct());
            }

            _discoveryContexts.Clear();
        }

        return _typeReferences;
    }

    public override void OnTypesInitialized(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (_agrInterceptors.Count > 0)
        {
            foreach (var interceptor in _agrInterceptors)
            {
                _discoveryContexts.Clear();

                foreach (var discoveryContext in discoveryContexts)
                {
                    if (interceptor.CanHandle(discoveryContext))
                    {
                        _discoveryContexts.Add(discoveryContext);
                    }
                }

                interceptor.OnTypesInitialized(_discoveryContexts);
            }

            _discoveryContexts.Clear();
        }
    }

    public override void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(discoveryContext))
            {
                interceptor.OnAfterRegisterDependencies(
                    discoveryContext, definition, contextData);
            }
        }
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(discoveryContext))
            {
                interceptor.OnBeforeRegisterDependencies(
                    discoveryContext, definition, contextData);
            }
        }
    }

    public override void OnBeforeCompleteTypeNames()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnBeforeCompleteTypeNames();
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnAfterCompleteTypeNames();
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(completionContext))
            {
                interceptor.OnBeforeCompleteName(completionContext, definition, contextData);
            }
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(completionContext))
            {
                interceptor.OnAfterCompleteName(completionContext, definition, contextData);
            }
        }
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _typeInterceptors)
        {
            if (interceptor.CanHandle(completionContext))
            {
                interceptor.OnAfterResolveRootType(
                    completionContext,
                    definition,
                    operationType,
                    contextData);
            }
        }
    }

    public override void OnTypesCompletedName(
        IReadOnlyCollection<ITypeCompletionContext> completionContexts)
    {
        if (_agrInterceptors.Count > 0)
        {
            foreach (var interceptor in _agrInterceptors)
            {
                _completionContexts.Clear();

                foreach (var completionContext in completionContexts)
                {
                    if (interceptor.CanHandle(completionContext))
                    {
                        _completionContexts.Add(completionContext);
                    }
                }

                interceptor.OnTypesCompletedName(_completionContexts);
            }

            _completionContexts.Clear();
        }
    }

    public override void OnBeforeMergeTypeExtensions()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnBeforeMergeTypeExtensions();
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnAfterMergeTypeExtensions();
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnBeforeCompleteTypes();
        }
    }

    public override void OnAfterCompleteTypes()
    {
        foreach (var interceptor in _flowInterceptors)
        {
            interceptor.OnAfterCompleteTypes();
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(completionContext))
            {
                interceptor.OnBeforeCompleteType(completionContext, definition, contextData);
            }
        }
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(completionContext))
            {
                interceptor.OnAfterCompleteType(completionContext, definition, contextData);
            }
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        foreach (var interceptor in _initInterceptors)
        {
            if (interceptor.CanHandle(validationContext))
            {
                interceptor.OnValidateType(validationContext, definition, contextData);
            }
        }
    }

    public override bool TryCreateScope(
        ITypeDiscoveryContext discoveryContext,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? typeDependencies)
    {
        foreach (var interceptor in _scopeInterceptors)
        {
            if (interceptor.TryCreateScope(discoveryContext, out typeDependencies))
            {
                return true;
            }
        }

        typeDependencies = null;
        return false;
    }

    public override void OnTypesCompleted(
        IReadOnlyCollection<ITypeCompletionContext> completionContexts)
    {
        if (_agrInterceptors.Count > 0)
        {
            foreach (var interceptor in _agrInterceptors)
            {
                _completionContexts.Clear();

                foreach (var completionContext in completionContexts)
                {
                    if (interceptor.CanHandle(completionContext))
                    {
                        _completionContexts.Add(completionContext);
                    }
                }

                interceptor.OnTypesCompleted(_completionContexts);
            }

            _completionContexts.Clear();
        }
    }
}
