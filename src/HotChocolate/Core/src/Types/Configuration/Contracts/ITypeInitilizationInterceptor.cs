using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration;

/// <summary>
/// A type initialization interceptors can hook into the various initialization events
/// of type system members and change / rewrite them. This is useful in order to transform
/// specified types.
/// </summary>
public interface ITypeInitializationInterceptor
{
    /// <summary>
    /// Defines if the type initialization shall trigger aggregated lifecycle
    /// events like OnTypesInitialized, OnTypesCompletedName and OnTypesCompleted.
    /// </summary>
    bool TriggerAggregations { get; }

    /// <summary>
    /// Specifies the types that this interceptor wants to handle.
    /// </summary>
    /// <param name="context">
    /// The type system context that represents a specific type system member.
    /// </param>
    /// <returns>
    /// <c>true</c> if this interceptor wants to handle the specified context;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool CanHandle(
        ITypeSystemObjectContext context);

    /// <summary>
    /// This event is triggered after the type instance was created but before
    /// any type definition was initialized.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    void OnBeforeInitialize(
        ITypeDiscoveryContext discoveryContext);

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
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

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
    IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts);

    /// <summary>
    /// This event is called after all type system members are initialized.
    /// </summary>
    /// <param name="discoveryContexts">
    /// The type discovery contexts that can be handled by this interceptor.
    /// </param>
    void OnTypesInitialized(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts);

    /// <summary>
    /// This event is called after the type definition is initialized
    /// but before the type dependencies are reported to the discovery contex.
    /// </summary>
    /// <param name="discoveryContext">
    /// The type discovery context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

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
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnAfterRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

    /// <summary>
    /// This event is called before the type name is assigned.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnBeforeCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

    /// <summary>
    /// This event is called after the type name is assigned.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

    /// <summary>
    /// This event is called after all type system members have been named.
    /// </summary>
    /// <param name="completionContexts">
    /// The type discovery contexts that can be handled by this interceptor.
    /// </param>
    void OnTypesCompletedName(
        IReadOnlyCollection<ITypeCompletionContext> completionContexts);

    /// <summary>
    /// This event is called before the type system member is fully completed.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

    /// <summary>
    /// This event is called after the type system member is fully completed.
    /// </summary>
    /// <param name="completionContext">
    /// The type completion context.
    /// </param>
    /// <param name="definition">
    /// The type definition of the type system member.
    /// </param>
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnAfterCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

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
    /// <param name="contextData">
    /// The context data of the type system member.
    /// </param>
    void OnValidateType(
        ITypeSystemObjectContext validationContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData);

    /// <summary>
    /// This event is called after all type system members have been completed.
    /// </summary>
    /// <param name="completionContexts">
    /// The type discovery contexts that can be handled by this interceptor.
    /// </param>
    void OnTypesCompleted(
        IReadOnlyCollection<ITypeCompletionContext> completionContexts);
}
