using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class AggregateTypeInterceptor : TypeInterceptor
{
    private readonly List<ITypeReference> _typeReferences = new();
    private TypeInterceptor[] _typeInterceptors;

    public AggregateTypeInterceptor()
    {
        _typeInterceptors = Array.Empty<TypeInterceptor>();
    }

    public void SetInterceptors(IReadOnlyCollection<object> interceptors)
    {
        _typeInterceptors = interceptors.OfType<TypeInterceptor>().ToArray();
    }

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).InitializeContext(
                context,
                typeInitializer,
                typeRegistry,
                typeLookup,
                typeReferenceResolver);
        }
    }

    public override void OnBeforeDiscoverTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeDiscoverTypes();
        }
    }

    public override void OnAfterDiscoverTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterDiscoverTypes();
        }
    }

    public override void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeInitialize(discoveryContext);
        }
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterInitialize(discoveryContext, definition);
        }
    }

    public override void OnTypeRegistered(ITypeDiscoveryContext discoveryContext)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnTypeRegistered(discoveryContext);
        }
    }

    public override IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        _typeReferences.Clear();

        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            _typeReferences.AddRange(
                Unsafe.Add(ref first, length).RegisterMoreTypes(discoveryContexts));
        }

        return _typeReferences;
    }

    public override void OnTypesInitialized()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnTypesInitialized();
        }
    }

    public override void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterRegisterDependencies(discoveryContext, definition);
        }
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length)
                .OnBeforeRegisterDependencies(discoveryContext, definition);
        }
    }

    public override void OnBeforeCompleteTypeNames()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeCompleteTypeNames();
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterCompleteTypeNames();
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeCompleteName(completionContext, definition);
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterCompleteName(completionContext, definition);
        }
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterResolveRootType(
                completionContext,
                definition,
                operationType);
        }
    }

    public override void OnTypesCompletedName()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnTypesCompletedName();
        }
    }

    public override void OnBeforeMergeTypeExtensions()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeMergeTypeExtensions();
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterMergeTypeExtensions();
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeCompleteTypes();
        }
    }

    public override void OnAfterCompleteTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterCompleteTypes();
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnBeforeCompleteType(completionContext, definition);
        }
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnAfterCompleteType(completionContext, definition);
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase? definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnValidateType(validationContext, definition);
        }
    }

    public override bool TryCreateScope(
        ITypeDiscoveryContext discoveryContext,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? typeDeps)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            if (Unsafe.Add(ref first, length).TryCreateScope(discoveryContext, out typeDeps))
            {
                return true;
            }
        }

        typeDeps = null;
        return false;
    }

    public override void OnTypesCompleted()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, length).OnTypesCompleted();
        }
    }

    private ref TypeInterceptor GetReference()
    {
#if NET6_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(_typeInterceptors);
#else
        return ref MemoryMarshal.GetReference(_typeInterceptors.AsSpan());
#endif
    }
}
