using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A base class for all GraphQL type system objects that have a type system configuration.
/// </summary>
public abstract class TypeSystemObjectBase<TConfiguration> : TypeSystemObjectBase
    where TConfiguration : TypeSystemConfiguration
{
    private TConfiguration? _configuration;
    private IReadOnlyDictionary<string, object?>? _contextData;

    public override IReadOnlyDictionary<string, object?> ContextData
        => _contextData ?? throw new TypeInitializationException();

    protected internal TConfiguration? Configuration
    {
        get => _configuration;
        protected set => _configuration = value;
    }

    internal sealed override void Initialize(ITypeDiscoveryContext context)
    {
        AssertUninitialized();

        OnBeforeInitialize(context);

        Scope = context.Scope;
        _configuration = CreateConfiguration(context);

        if (_configuration is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }

        // if we at this point already know the name we will just commit it.
        if (!string.IsNullOrEmpty(_configuration.Name))
        {
            Name = _configuration.Name;
        }

        RegisterConfigurationDependencies(context, _configuration);

        OnAfterInitialize(context, _configuration);

        MarkInitialized();
    }

    protected abstract TConfiguration CreateConfiguration(
        ITypeDiscoveryContext context);

    protected virtual void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        TConfiguration configuration) { }

    internal sealed override void CompleteName(ITypeCompletionContext context)
    {
        AssertInitialized();

        var config = _configuration!;

        OnBeforeCompleteName(context, config);

        ExecuteConfigurations(context, config, ApplyConfigurationOn.BeforeNaming);
        OnCompleteName(context, config);

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

        OnAfterCompleteName(context, config);
        ExecuteConfigurations(context, config, ApplyConfigurationOn.AfterNaming);

        MarkNamed();
    }

    protected virtual void OnCompleteName(
        ITypeCompletionContext context,
        TConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(configuration.Name))
        {
            Name = configuration.Name;
        }
    }

    internal sealed override void CompleteType(ITypeCompletionContext context)
    {
        AssertNamed();

        var config = _configuration!;

        OnBeforeCompleteType(context, config);

        ExecuteConfigurations(context, config, ApplyConfigurationOn.BeforeCompletion);
        Description = config.Description;
        OnCompleteType(context, config);

        _contextData = config.GetContextData();

        OnAfterCompleteType(context, config);
        ExecuteConfigurations(context, config, ApplyConfigurationOn.AfterCompletion);

        MarkCompleted();
    }

    protected virtual void OnCompleteType(
        ITypeCompletionContext context,
        TConfiguration configuration) { }

    internal sealed override void CompleteMetadata(ITypeCompletionContext context)
    {
        AssertTypeCompleted();

        var config = _configuration!;

        OnBeforeCompleteMetadata(context, config);
        OnCompleteMetadata(context, config);
        OnAfterCompleteMetadata(context, config);

        MarkMetadataCompleted();
    }

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        TConfiguration configuration) { }

    internal sealed override void MakeExecutable(ITypeCompletionContext context)
    {
        AssertMetadataCompleted();

        var definition = _configuration!;

        OnBeforeMakeExecutable(context, definition);
        OnMakeExecutable(context, definition);
        OnAfterMakeExecutable(context, definition);

        MarkExecutable();
    }

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        TConfiguration configuration) { }

    protected virtual void OnFinalizeType(
        ITypeCompletionContext context,
        TConfiguration configuration) { }

    internal sealed override void FinalizeType(ITypeCompletionContext context)
    {
        // first we will call the OnFinalizeType hook.
        OnFinalizeType(context, _configuration!);
        var config = _configuration!;

        // next we will release the configuration here so that it can be collected by the GC.
        _configuration = null;

        // if the ExtensionData object has no data, we will release it so it can be
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

        OnValidateType(context, config);

        MarkFinalized();
    }

    private void RegisterConfigurationDependencies(
        ITypeDiscoveryContext context,
        TConfiguration configuration)
    {
        OnBeforeRegisterDependencies(context, configuration);

        foreach (var task in configuration.GetTasks())
        {
            foreach (var dependency in task.Dependencies)
            {
                context.Dependencies.Add(dependency);
            }
        }

        OnRegisterDependencies(context, configuration);
        OnAfterRegisterDependencies(context, configuration);
    }

    private static void ExecuteConfigurations(
        ITypeCompletionContext context,
        TConfiguration configuration,
        ApplyConfigurationOn on)
    {
        foreach (var task in configuration.GetTasks())
        {
            if (task.On == on)
            {
                ((OnCompleteTypeSystemConfigurationTask)task).Configure(context);
            }
        }
    }

    protected virtual void OnBeforeInitialize(
        ITypeDiscoveryContext context)
        => context.TypeInterceptor.OnBeforeInitialize(context);

    protected virtual void OnAfterInitialize(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterInitialize(context, configuration);

    protected virtual void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnBeforeRegisterDependencies(context, configuration);

    protected virtual void OnAfterRegisterDependencies(
        ITypeDiscoveryContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterRegisterDependencies(context, configuration);

    protected virtual void OnBeforeCompleteName(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnBeforeCompleteName(context, configuration);

    protected virtual void OnAfterCompleteName(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterCompleteName(context, configuration);

    protected virtual void OnBeforeCompleteType(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnBeforeCompleteType(context, configuration);

    protected virtual void OnAfterCompleteType(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterCompleteType(context, configuration);

    protected virtual void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnBeforeCompleteMetadata(context, configuration);

    protected virtual void OnAfterCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterCompleteMetadata(context, configuration);

    protected virtual void OnBeforeMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnBeforeMakeExecutable(context, configuration);

    protected virtual void OnAfterMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnAfterMakeExecutable(context, configuration);

    protected virtual void OnValidateType(
        ITypeSystemObjectContext context,
        TypeSystemConfiguration configuration)
        => context.TypeInterceptor.OnValidateType(context, configuration);

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
            _configuration is not null,
            "Initialize must have been invoked before completing the type name.");

        if (!IsInitialized)
        {
            throw new InvalidOperationException();
        }

        if (_configuration is null)
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
            _configuration?.Name is not null,
            "The name must have been completed before completing the type.");

        if (!IsNamed)
        {
            throw new InvalidOperationException();
        }

        if (_configuration is null)
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

        if (_configuration is null)
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

        if (_configuration is null)
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
