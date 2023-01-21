using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// A type initialization interceptors can hook into the various initialization events
/// of type system members and change / rewrite them. This is useful in order to transform
/// specified types.
/// </summary>
public abstract class TypeInterceptor
{
    private const int _position = 0;

    /// <summary>
    /// A weight to order interceptors.
    /// </summary>
    internal virtual uint Position => _position;

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
    public virtual void OnBeforeCreateSchema(
        IDescriptorContext context,
        ISchemaBuilder schemaBuilder) { }

    internal virtual void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver) { }

    /// <summary>
    /// This method is called before the type discovery is started.
    /// </summary>
    public virtual void OnBeforeDiscoverTypes() { }

    /// <summary>
    /// This method is called after the type discovery is finished.
    /// </summary>
    public virtual void OnAfterDiscoverTypes() { }

    /// <summary>
    /// This event is triggered after the type instance was created but before
    /// any type definition was initialized.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    public virtual void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext) { }

    /// <summary>
    /// This event is triggered after the type type definition was initialized and
    /// after the dependencies of this type have been registered
    /// with the type discovery context.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition) { }

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
        => Enumerable.Empty<TypeReference>();

    public virtual void OnTypeRegistered(
        ITypeDiscoveryContext discoveryContext) { }

    public virtual void OnTypesInitialized() { }

    /// <summary>
    /// This event is called after the type definition is initialized
    /// but before the type dependencies are reported to the discovery context.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition) { }

    /// <summary>
    /// This event is called after the type dependencies are reported to the
    /// type discovery context but before the type definition is fully initialized.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition) { }

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
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition) { }

    /// <summary>
    /// This event is called after the type name is assigned.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition) { }

    internal virtual void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition,
        OperationType operationType) { }

    public virtual void OnTypesCompletedName() { }

    /// <summary>
    /// This method is called before the type extensions are merged.
    /// </summary>
    public virtual void OnBeforeMergeTypeExtensions() { }

    /// <summary>
    /// This method is called after the type extensions are merged.
    /// </summary>
    public virtual void OnAfterMergeTypeExtensions() { }

    /// <summary>
    /// This method is called before the types are completed.
    /// </summary>
    public virtual void OnBeforeCompleteTypes() { }

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
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition) { }

    /// <summary>
    /// This event is called after the type system member is fully completed.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition) { }

    /// <summary>
    /// This event is called after the type system member is fully completed and is
    /// intended to add validation logic. If the type is not valid throw a
    /// <see cref="SchemaException"/>.
    /// </summary>
    /// <param name="validationContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    public virtual void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase definition) { }

    public virtual void OnTypesCompleted() { }

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
    public virtual void OnAfterCreateSchema(IDescriptorContext context, ISchema schema) { }

    /// <summary>
    /// This hook is invoked if an error occured during schema creation.
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
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="typeDependencies">
    ///
    /// </param>
    public virtual bool TryCreateScope(
        ITypeDiscoveryContext discoveryContext,
        [NotNullWhen(true)] out IReadOnlyList<TypeDependency>? typeDependencies)
    {
        typeDependencies = null;
        return false;
    }
}
