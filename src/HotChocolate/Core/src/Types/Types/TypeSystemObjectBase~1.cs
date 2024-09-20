using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public abstract class TypeSystemObjectBase<TDefinition> : TypeSystemObjectBase
    where TDefinition : DefinitionBase
{
    private TDefinition? _definition;
    private IReadOnlyDictionary<string, object?>? _contextData;

    public override IReadOnlyDictionary<string, object?> ContextData
        => _contextData ?? throw new TypeInitializationException();

    protected internal TDefinition? Definition
    {
        get => _definition;
        protected set => _definition = value;
    }

    internal sealed override void Initialize(ITypeDiscoveryContext context)
    {
        AssertUninitialized();

        OnBeforeInitialize(context);

        Scope = context.Scope;
        _definition = CreateDefinition(context);

        if (_definition is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }

        // if we at this point already know the name we will just commit it.
        if (!string.IsNullOrEmpty(_definition.Name))
        {
            Name = _definition.Name;
        }

        RegisterConfigurationDependencies(context, _definition);

        OnAfterInitialize(context, _definition);

        MarkInitialized();
    }

    protected abstract TDefinition CreateDefinition(
        ITypeDiscoveryContext context);

    protected virtual void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        TDefinition definition) { }

    internal sealed override void CompleteName(ITypeCompletionContext context)
    {
        AssertInitialized();

        var definition = _definition!;

        OnBeforeCompleteName(context, definition);

        ExecuteConfigurations(context, definition, ApplyConfigurationOn.BeforeNaming);
        OnCompleteName(context, definition);

        Debug.Assert(
            !string.IsNullOrEmpty(Name),
            "After the naming is completed the name has to have a value.");

        if (string.IsNullOrEmpty(Name))
        {
            context.ReportError(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        TypeResources.TypeSystemObjectBase_NameIsNull,
                        GetType().FullName)
                    .SetCode(ErrorCodes.Schema.NoName)
                    .SetTypeSystemObject(this)
                    .Build());
        }

        OnAfterCompleteName(context, definition);
        ExecuteConfigurations(context, definition, ApplyConfigurationOn.AfterNaming);

        MarkNamed();
    }

    protected virtual void OnCompleteName(
        ITypeCompletionContext context,
        TDefinition definition)
    {
        if (!string.IsNullOrEmpty(definition.Name))
        {
            Name = definition.Name;
        }
    }

    internal sealed override void CompleteType(ITypeCompletionContext context)
    {
        AssertNamed();

        var definition = _definition!;

        OnBeforeCompleteType(context, definition);

        ExecuteConfigurations(context, definition, ApplyConfigurationOn.BeforeCompletion);
        Description = definition.Description;
        OnCompleteType(context, definition);

        _contextData = definition.GetContextData();

        OnAfterCompleteType(context, definition);
        ExecuteConfigurations(context, definition, ApplyConfigurationOn.AfterCompletion);

        OnValidateType(context, definition);

        MarkCompleted();
    }

    internal sealed override void FinalizeType(ITypeCompletionContext context)
    {
        // we will release the definition here so that it can be collected by the GC.
        _definition = null;

        // if the ExtensionData object has no data we will release it so it can be
        // collected by the GC.
        if (_contextData!.Count == 0 && _contextData is not ImmutableDictionary<string, object?>)
        {
            _contextData = ImmutableDictionary<string, object?>.Empty;
        }

        // if contextData is still wrapped we will unwrap it here so that access is faster without
        // any null checking.
        else if (_contextData is ExtensionData extensionData &&
            extensionData.TryGetInnerDictionary(out var dictionary))
        {
            _contextData = dictionary;
        }

        MarkFinalized();
    }

    protected virtual void OnCompleteType(
        ITypeCompletionContext context,
        TDefinition definition) { }

    private void RegisterConfigurationDependencies(
        ITypeDiscoveryContext context,
        TDefinition definition)
    {
        OnBeforeRegisterDependencies(context, definition);

        foreach (var configuration in definition.GetConfigurations())
        {
            foreach (var dependency in configuration.Dependencies)
            {
                context.Dependencies.Add(dependency);
            }
        }

        OnRegisterDependencies(context, definition);
        OnAfterRegisterDependencies(context, definition);
    }

    private static void ExecuteConfigurations(
        ITypeCompletionContext context,
        TDefinition definition,
        ApplyConfigurationOn on)
    {
        foreach (var config in definition.GetConfigurations())
        {
            if (config.On == on)
            {
                ((CompleteConfiguration)config).Configure(context);
            }
        }
    }

    protected virtual void OnBeforeInitialize(
        ITypeDiscoveryContext context)
        => context.TypeInterceptor.OnBeforeInitialize(context);

    protected virtual void OnAfterInitialize(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnAfterInitialize(context, definition);

    protected virtual void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnBeforeRegisterDependencies(context, definition);

    protected virtual void OnAfterRegisterDependencies(
        ITypeDiscoveryContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnAfterRegisterDependencies(context, definition);

    protected virtual void OnBeforeCompleteName(
        ITypeCompletionContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnBeforeCompleteName(context, definition);

    protected virtual void OnAfterCompleteName(
        ITypeCompletionContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnAfterCompleteName(context, definition);

    protected virtual void OnBeforeCompleteType(
        ITypeCompletionContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnBeforeCompleteType(context, definition);

    protected virtual void OnAfterCompleteType(
        ITypeCompletionContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnAfterCompleteType(context, definition);

    protected virtual void OnValidateType(
        ITypeSystemObjectContext context,
        DefinitionBase definition)
        => context.TypeInterceptor.OnValidateType(context, definition);

    private void AssertUninitialized()
    {
        Debug.Assert(
            !IsInitialized,
            "The type must be uninitialized.");

        if (IsInitialized)
        {
            throw new InvalidOperationException();
        }
    }

    private void AssertInitialized()
    {
        Debug.Assert(
            IsInitialized,
            "The type must be initialized.");

        Debug.Assert(
            _definition is not null,
            "Initialize must have been invoked before completing the type name.");

        if (!IsInitialized)
        {
            throw new InvalidOperationException();
        }

        if (_definition is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }
    }

    private void AssertNamed()
    {
        Debug.Assert(
            IsNamed,
            "The type must be initialized.");

        Debug.Assert(
            _definition?.Name is not null,
            "The name must have been completed before completing the type.");

        if (!IsNamed)
        {
            throw new InvalidOperationException();
        }

        if (_definition is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }
    }

    protected internal void AssertMutable()
    {
        Debug.Assert(
            !IsCompleted,
            "The type os no longer mutable.");

        if (IsCompleted)
        {
            throw new InvalidOperationException("The type is no longer mutable.");
        }
    }
}
