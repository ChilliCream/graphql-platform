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
    private readonly List<TypeReference> _typeReferences = new();
    private TypeInterceptor[] _typeInterceptors;

    public AggregateTypeInterceptor()
    {
        _typeInterceptors = Array.Empty<TypeInterceptor>();
    }

    public void SetInterceptors(IReadOnlyCollection<TypeInterceptor> typeInterceptors)
    {
        _typeInterceptors = new TypeInterceptor[typeInterceptors.Count];
        var i = 0;

        foreach (var typeInterceptor in typeInterceptors.OrderBy(t => t.Position))
        {
            _typeInterceptors[i++] = typeInterceptor;
        }
    }

    public override void OnBeforeCreateSchema(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCreateSchema(context, schemaBuilder);
        }
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
            Unsafe.Add(ref first, i).InitializeContext(
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
            Unsafe.Add(ref first, i).OnBeforeDiscoverTypes();
        }
    }

    public override void OnAfterDiscoverTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterDiscoverTypes();
        }
    }

    public override void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeInitialize(discoveryContext);
        }
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterInitialize(discoveryContext, definition);
        }
    }

    public override void OnTypeRegistered(ITypeDiscoveryContext discoveryContext)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnTypeRegistered(discoveryContext);
        }
    }

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        _typeReferences.Clear();

        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            _typeReferences.AddRange(
                Unsafe.Add(ref first, i).RegisterMoreTypes(discoveryContexts));
        }

        return _typeReferences;
    }

    public override void OnTypesInitialized()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnTypesInitialized();
        }
    }

    public override void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterRegisterDependencies(discoveryContext, definition);
        }
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeRegisterDependencies(discoveryContext, definition);
        }
    }

    public override void OnBeforeCompleteTypeNames()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteTypeNames();
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteTypeNames();
        }
    }

    public override void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteName(completionContext, definition);
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteName(completionContext, definition);
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
            Unsafe.Add(ref first, i).OnAfterResolveRootType(
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
            Unsafe.Add(ref first, i).OnTypesCompletedName();
        }
    }

    public override void OnBeforeMergeTypeExtensions()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeMergeTypeExtensions();
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterMergeTypeExtensions();
        }
    }

    public override void OnBeforeCompleteTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteTypes();
        }
    }

    public override void OnAfterCompleteTypes()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteTypes();
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteType(completionContext, definition);
        }
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteType(completionContext, definition);
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase definition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnValidateType(validationContext, definition);
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
            if (Unsafe.Add(ref first, i).TryCreateScope(discoveryContext, out typeDeps))
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
            Unsafe.Add(ref first, i).OnTypesCompleted();
        }
    }

    public override void OnAfterCreateSchema(IDescriptorContext context, ISchema schema)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCreateSchema(context, schema);
        }
    }

    public override void OnCreateSchemaError(IDescriptorContext context, Exception error)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnCreateSchemaError(context, error);
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

