using System.Diagnostics;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A base class for all GraphQL type system objects that have a type system configuration.
/// </summary>
public abstract class TypeSystemObject<TConfiguration> : TypeSystemObject
    where TConfiguration : TypeSystemConfiguration
{
    private IFeatureCollection? _features;

    public override IFeatureCollection Features
        => _features ?? throw new TypeInitializationException();

    protected internal TConfiguration? Configuration { get; protected set; }

    internal sealed override void Initialize(ITypeDiscoveryContext context)
    {
        AssertUninitialized();

        OnBeforeInitialize(context);

        Scope = context.Scope;
        Configuration = CreateConfiguration(context);

        if (Configuration is null)
        {
            throw new InvalidOperationException(
                TypeResources.TypeSystemObjectBase_DefinitionIsNull);
        }

        // if we at this point already know the name, we will just commit it.
        if (!string.IsNullOrEmpty(Configuration.Name))
        {
            Name = Configuration.Name;
        }

        RegisterConfigurationDependencies(context, Configuration);

        OnAfterInitialize(context, Configuration);

        MarkInitialized();
    }

    protected abstract TConfiguration CreateConfiguration(
        ITypeDiscoveryContext context);

    protected virtual void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        TConfiguration configuration)
    { }

    internal sealed override void CompleteName(ITypeCompletionContext context)
    {
        AssertInitialized();

        var config = Configuration!;

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

        var config = Configuration!;

        OnBeforeCompleteType(context, config);

        ExecuteConfigurations(context, config, ApplyConfigurationOn.BeforeCompletion);
        Description = config.Description;
        OnCompleteType(context, config);

        _features = config.GetFeatures();

        OnAfterCompleteType(context, config);
        ExecuteConfigurations(context, config, ApplyConfigurationOn.AfterCompletion);

        MarkCompleted();
    }

    protected virtual void OnCompleteType(
        ITypeCompletionContext context,
        TConfiguration configuration)
    { }

    internal sealed override void CompleteMetadata(ITypeCompletionContext context)
    {
        AssertTypeCompleted();

        var config = Configuration!;

        OnBeforeCompleteMetadata(context, config);
        OnCompleteMetadata(context, config);
        OnAfterCompleteMetadata(context, config);

        MarkMetadataCompleted();
    }

    protected virtual void OnCompleteMetadata(
        ITypeCompletionContext context,
        TConfiguration configuration)
    { }

    internal sealed override void MakeExecutable(ITypeCompletionContext context)
    {
        AssertMetadataCompleted();

        var definition = Configuration!;

        OnBeforeMakeExecutable(context, definition);
        OnMakeExecutable(context, definition);
        OnAfterMakeExecutable(context, definition);

        MarkExecutable();
    }

    protected virtual void OnMakeExecutable(
        ITypeCompletionContext context,
        TConfiguration configuration)
    { }

    protected virtual void OnFinalizeType(
        ITypeCompletionContext context,
        TConfiguration configuration)
    { }

    internal sealed override void FinalizeType(ITypeCompletionContext context)
    {
        // first we will call the OnFinalizeType hook.
        OnFinalizeType(context, Configuration!);
        var config = Configuration!;

        // next we will release the configuration here so that it can be collected by the GC.
        Configuration = null;
        _features = _features?.ToReadOnly() ?? FeatureCollection.Empty;

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
            Configuration is not null,
            "Initialize must have been invoked before completing the type name.");

        if (!IsInitialized)
        {
            throw new InvalidOperationException();
        }

        if (Configuration is null)
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
            Configuration?.Name is not null,
            "The name must have been completed before completing the type.");

        if (!IsNamed)
        {
            throw new InvalidOperationException();
        }

        if (Configuration is null)
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

        if (Configuration is null)
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

        if (Configuration is null)
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
