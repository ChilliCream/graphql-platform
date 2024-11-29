using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

internal sealed class AggregateTypeInterceptor : TypeInterceptor
{
    private readonly List<TypeReference> _typeReferences = [];
    private TypeInterceptor[] _typeInterceptors = [];
    private TypeInterceptor? _mutationAggregator;

    public void SetInterceptors(IReadOnlyCollection<TypeInterceptor> typeInterceptors)
    {
        _typeInterceptors = new TypeInterceptor[typeInterceptors.Count];
        var i = 0;

        foreach (var typeInterceptor in typeInterceptors.OrderBy(t => t.Position))
        {
            _typeInterceptors[i++] = typeInterceptor;
        }

        foreach (var interceptor in _typeInterceptors)
        {
            interceptor.SetSiblings(_typeInterceptors);
        }
    }

    internal override void OnBeforeCreateSchemaInternal(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
        ref var start = ref GetReference();
        ref var current = ref Unsafe.Add(ref start, 0);
        ref var end = ref Unsafe.Add(ref current, _typeInterceptors.Length);

        // we first initialize all schema context ...
        while (Unsafe.IsAddressLessThan(ref current, ref end))
        {
            current.OnBeforeCreateSchemaInternal(context, schemaBuilder);
            current = ref Unsafe.Add(ref current, 1)!;
        }

        current = ref Unsafe.Add(ref start, 0)!;
        var i = 0;
        TypeInterceptor[]? temp = null;

        // next we determine the type interceptors that are enabled ...
        while (Unsafe.IsAddressLessThan(ref current, ref end))
        {
            var enabled = current.IsEnabled(context);

            if (temp is null && !enabled)
            {
                temp ??= new TypeInterceptor[_typeInterceptors.Length];
                ref var next = ref Unsafe.Add(ref start, 0);
                while (Unsafe.IsAddressLessThan(ref next, ref current))
                {
                    temp[i++] = next;
                    next = ref Unsafe.Add(ref next, 1)!;
                }
            }

            if (enabled)
            {
                if (temp is not null)
                {
                    temp[i++] = current;
                }

                if (_mutationAggregator is null && current.IsMutationAggregator(context))
                {
                    _mutationAggregator = current;
                }
            }

            current = ref Unsafe.Add(ref current, 1)!;
        }

        if (temp is not null)
        {
            Array.Resize(ref temp, i);
            _typeInterceptors = temp;
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

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
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

    internal override void OnBeforeCompleteMutation(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition)
    {
        if (_mutationAggregator is not null)
        {
            _mutationAggregator.OnBeforeCompleteMutation(completionContext, definition);
            return;
        }

        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteMutation(completionContext, definition);
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

    internal override void OnBeforeRegisterSchemaTypes(
        IDescriptorContext context,
        SchemaTypesDefinition schemaTypesDefinition)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeRegisterSchemaTypes(context, schemaTypesDefinition);
        }
    }

    internal override void OnAfterCreateSchemaInternal(IDescriptorContext context, ISchema schema)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCreateSchemaInternal(context, schema);
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
        return ref MemoryMarshal.GetArrayDataReference(_typeInterceptors);
    }
}
