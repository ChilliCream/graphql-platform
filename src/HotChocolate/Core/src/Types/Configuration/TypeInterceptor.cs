using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Configuration;

// note: this type is considered internal and should not be used by users.
/// <summary>
/// A type initialization interceptor can hook into the various initialization events
/// of type system objects. This is useful to transform type system shape and behavior.
/// </summary>
public abstract class TypeInterceptor
{
    private const uint DefaultPosition = uint.MaxValue / 2;

    /// <summary>
    /// A weight to order interceptors.
    /// </summary>
    internal virtual uint Position => DefaultPosition;

    public virtual bool IsEnabled(IDescriptorContext context) => true;

    internal virtual bool IsMutationAggregator(IDescriptorContext context) => false;

    internal virtual void SetSiblings(TypeInterceptor[] all) { }

    // note: this hook is a legacy hook and will be removed once the new schema building API is completed.
    /// <summary>
    /// This hook is invoked before anything else any allows for additional modification
    /// with the schema builder.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="schemaBuilder">
    /// The schema builder.
    /// </param>
    internal virtual void OnBeforeCreateSchemaInternal(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder)
    {
    }

    internal virtual void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
    }

    /// <summary>
    /// This method is called before the type discovery is started.
    /// </summary>
    public virtual void OnBeforeDiscoverTypes() { }

    /// <summary>
    /// This method is called after the type discovery is finished.
    /// </summary>
    public virtual void OnAfterDiscoverTypes() { }

    internal virtual bool SkipDirectiveDefinition(DirectiveDefinitionNode node)
        => false;

    /// <summary>
    /// This event is triggered after the type instance was created but before
    /// any type system configuration is initialized.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    public virtual void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext)
    {
    }

    /// <summary>
    /// This event is triggered after the type system configuration was initialized and
    /// after the dependencies of this type have been registered
    /// with the type discovery context.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// If all types are registered you can analyze them and add more new types at this point.
    /// This event could be hit multiple times.
    ///
    /// Ones <see cref="OnTypesInitialized"/> is hit, no more types can be added.
    /// </summary>
    /// <param name="discoveryContexts">
    /// The discovery contexts of types that are already initialized.
    /// </param>
    /// <returns>
    /// Returns types that shall be included into the schema.
    /// </returns>
    public virtual IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        => [];

    public virtual void OnTypeRegistered(
        ITypeDiscoveryContext discoveryContext)
    {
    }

    /// <summary>
    /// This event is called after all types are initialized.
    /// </summary>
    public virtual void OnTypesInitialized() { }

    /// <summary>
    /// This event is called after the type system configuration is initialized
    /// but before the type dependencies are reported to the discovery context.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the type dependencies are reported to the
    /// type discovery context but before the type system configuration
    /// is fully initialized.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This method is called before the type names are completed.
    /// </summary>
    public virtual void OnBeforeCompleteTypeNames() { }

    /// <summary>
    /// This method is called after the type names are completed.
    /// </summary>
    public virtual void OnAfterCompleteTypeNames() { }

    /// <summary>
    /// This event is called before the type name is assigned.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the type name is assigned.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the root type is resolved.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    /// <param name="operationType">
    /// Specifies what kind of operation type is resolved.
    /// </param>
    [Experimental(Experiments.RootTypeResolved)]
    public virtual void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration,
        OperationType operationType)
    {
    }

    /// <summary>
    /// This event is called after the type name is assigned.
    /// </summary>
    public virtual void OnTypesCompletedName() { }

    /// <summary>
    /// This method is called before the type extensions are merged.
    /// </summary>
    public virtual void OnBeforeMergeTypeExtensions() { }

    /// <summary>
    /// This method is called after the type extensions are merged.
    /// </summary>
    public virtual void OnAfterMergeTypeExtensions() { }

    internal virtual void OnBeforeCompleteMutation(
        ITypeCompletionContext completionContext,
        ObjectTypeConfiguration configuration)
    {
        foreach (var field in configuration.Fields)
        {
            OnBeforeCompleteMutationField(completionContext, field);
        }
    }

    public virtual void OnBeforeCompleteMutationField(
        ITypeCompletionContext completionContext,
        ObjectFieldConfiguration mutationField)
    {
    }

    /// <summary>
    /// This method is called before the types are completed.
    /// </summary>
    public virtual void OnBeforeCompleteTypes() { }

    /// <summary>
    /// This method is called after the types are completed.
    /// </summary>
    public virtual void OnTypesCompleted() { }

    /// <summary>
    /// This method is called after the types are completed.
    /// </summary>
    public virtual void OnAfterCompleteTypes() { }

    /// <summary>
    /// This event is called before the type system member is fully completed.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the type system member is fully completed.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This method is called before the metadata of all types are completed.
    /// </summary>
    public virtual void OnBeforeCompleteMetadata() { }

    /// <summary>
    /// This method is called after the metadata of all types are completed.
    /// </summary>
    public virtual void OnAfterCompleteMetadata() { }

    /// <summary>
    /// This event is called before the metadata of the type system member is fully completed.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnBeforeCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the metadata of the type system member was fully completed.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterCompleteMetadata(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This method is called before the types are made executable.
    /// </summary>
    public virtual void OnBeforeMakeExecutable() { }

    /// <summary>
    /// This method is called after the types are made executable.
    /// </summary>
    public virtual void OnAfterMakeExecutable() { }

    /// <summary>
    /// This event is called before the type system member is made executable.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnBeforeMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the type system member is made executable.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnAfterMakeExecutable(
        ITypeCompletionContext context,
        TypeSystemConfiguration configuration)
    {
    }

    /// <summary>
    /// This event is called after the type system member is fully completed and is
    /// intended to add validation logic. If the type is not valid throw a
    /// <see cref="SchemaException"/>.
    /// </summary>
    /// <param name="context">
    /// The type completion context.
    /// </param>
    /// <param name="configuration">
    /// The type system configuration of the type system member.
    /// </param>
    public virtual void OnValidateType(
        ITypeSystemObjectContext context,
        TypeSystemConfiguration configuration)
    {
    }

    // note: this hook is a legacy hook and will be removed once the new schema building API is completed.
    /// <summary>
    /// This hook is invoked after schema is fully created and gives access
    /// to the created schema object.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="configuration">
    /// The schema types configuration.
    /// </param>
    internal virtual void OnBeforeRegisterSchemaTypes(
        IDescriptorContext context,
        SchemaTypesConfiguration configuration)
    {
    }

    // note: this hook is a legacy hook and will be removed once the new schema building API is completed.
    /// <summary>
    /// This hook is invoked after schema is fully created and gives access
    /// to the created schema object.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="schema">
    /// The created schema.
    /// </param>
    internal virtual void OnAfterCreateSchemaInternal(IDescriptorContext context, Schema schema)
    {
    }

    /// <summary>
    /// This hook is invoked if an error occurred during schema creation.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="error">
    /// The error.
    /// </param>
    public virtual void OnCreateSchemaError(IDescriptorContext context, Exception error) { }

    /// <summary>
    /// This event is called after the type was registered with the type registry.
    /// </summary>
    /// <param name="context">
    /// The type discovery context.
    /// </param>
    /// <param name="dependencies">
    ///
    /// </param>
    public virtual bool TryCreateScope(
        ITypeDiscoveryContext context,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? dependencies)
    {
        dependencies = null;
        return false;
    }
}
