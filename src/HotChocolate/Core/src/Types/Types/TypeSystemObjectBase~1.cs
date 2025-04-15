using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public abstract class TypeSystemObjectBase<TDefinition> : TypeSystemObjectBase
    where TDefinition : TypeSystemConfiguration
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

        MarkCompleted();
    }

    protected virtual void OnCompleteType(
        ITypeCompletionContext context,
        TDefinition definition) { }

    internal sealed override void CompleteMetadata(ITypeCompletionContext context)
    {
        AssertTypeCompleted();

        var definition = _definition!;

        OnBeforeCompleteMetadata(context, definition);
        OnCompleteMetadata(context, definition);
        OnAfterCompleteMetadata(context, definition);

        MarkMetadataCompleted();
    }

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        TDefinition definition) { }

    internal sealed override void MakeExecutable(ITypeCompletionContext context)
    {
        AssertMetadataCompleted();

        var definition = _definition!;

        OnBeforeMakeExecutable(context, definition);
        OnMakeExecutable(context, definition);
        OnAfterMakeExecutable(context, definition);

        MarkExecutable();
    }

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        TDefinition definition) { }

    protected virtual void OnFinalizeType(
        ITypeCompletionContext context,
        TDefinition definition) { }

    internal sealed override void FinalizeType(ITypeCompletionContext context)
    {
        // first we will call the OnFinalizeType hook.
        OnFinalizeType(context, _definition!);
        var definition = _definition!;

        // next we will release the definition here so that it can be collected by the GC.
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

        OnValidateType(context, definition);

        MarkFinalized();
    }

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
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterInitialize(context, definition);

    protected virtual void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnBeforeRegisterDependencies(context, definition);

    protected virtual void OnAfterRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterRegisterDependencies(context, definition);

    protected virtual void OnBeforeCompleteName(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnBeforeCompleteName(context, definition);

    protected virtual void OnAfterCompleteName(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterCompleteName(context, definition);

    protected virtual void OnBeforeCompleteType(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnBeforeCompleteType(context, definition);

    protected virtual void OnAfterCompleteType(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterCompleteType(context, definition);

    protected virtual void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnBeforeCompleteMetadata(context, definition);

    protected virtual void OnAfterCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterCompleteMetadata(context, definition);

    protected virtual void OnBeforeMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnBeforeMakeExecutable(context, definition);

    protected virtual void OnAfterMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration definition)
        => context.TypeInterceptor.OnAfterMakeExecutable(context, definition);

    protected virtual void OnValidateType(
        ITypeSystemObjectContext context,
        TypeSystemConfiguration definition)
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

    private void AssertTypeCompleted()
    {
        Debug.Assert(
            IsCompleted,
            "The type must be initialized.");

        if (!IsCompleted)
        {
            throw new InvalidOperationException();
        }

        if (_definition is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }
    }

    private void AssertMetadataCompleted()
    {
        Debug.Assert(
            IsMetadataCompleted,
            "The type must be initialized.");

        if (!IsMetadataCompleted)
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
            !IsExecutable,
            "The type os no longer mutable.");

        if (IsExecutable)
        {
            throw new InvalidOperationException("The type is no longer mutable.");
        }
    }
}
