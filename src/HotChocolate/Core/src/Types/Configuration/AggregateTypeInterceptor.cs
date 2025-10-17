using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

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

    internal override bool SkipDirectiveDefinition(DirectiveDefinitionNode node)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            if (Unsafe.Add(ref first, i).SkipDirectiveDefinition(node))
            {
                return true;
            }
        }

        return false;
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
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterInitialize(discoveryContext, configuration);
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
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterRegisterDependencies(discoveryContext, configuration);
        }
    }

    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeRegisterDependencies(discoveryContext, configuration);
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
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteName(completionContext, configuration);
        }
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteName(completionContext, configuration);
        }
    }

    public override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterResolveRootType(
                completionContext,
                configuration,
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
        ObjectTypeConfiguration configuration)
    {
        if (_mutationAggregator is not null)
        {
            _mutationAggregator.OnBeforeCompleteMutation(completionContext, configuration);
            return;
        }

        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteMutation(completionContext, configuration);
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
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteType(completionContext, configuration);
        }
    }

    public override void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteType(completionContext, configuration);
        }
    }

    public override void OnBeforeCompleteMetadata()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteMetadata();
        }
    }

    public override void OnAfterCompleteMetadata()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteMetadata();
        }
    }

    public override void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeCompleteMetadata(context, configuration);
        }
    }

    public override void OnAfterCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterCompleteMetadata(context, configuration);
        }
    }

    public override void OnBeforeMakeExecutable()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeMakeExecutable();
        }
    }

    public override void OnAfterMakeExecutable()
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterMakeExecutable();
        }
    }

    public override void OnBeforeMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeMakeExecutable(context, configuration);
        }
    }

    public override void OnAfterMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnAfterMakeExecutable(context, configuration);
        }
    }

    public override void OnValidateType(
        ITypeSystemObjectContext context,
        TypeSystemConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnValidateType(context, configuration);
        }
    }

    public override bool TryCreateScope(
        ITypeDiscoveryContext context,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? dependencies)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            if (Unsafe.Add(ref first, i).TryCreateScope(context, out dependencies))
            {
                return true;
            }
        }

        dependencies = null;
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
        SchemaTypesConfiguration configuration)
    {
        ref var first = ref GetReference();
        var length = _typeInterceptors.Length;

        for (var i = 0; i < length; i++)
        {
            Unsafe.Add(ref first, i).OnBeforeRegisterSchemaTypes(context, configuration);
        }
    }

    internal override void OnAfterCreateSchemaInternal(IDescriptorContext context, Schema schema)
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
