using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// The resolver context represent the execution context for a specific
/// field that is being resolved.
/// </summary>
public interface IResolverContext : IHasContextData
{
    /// <summary>
    /// Gets the GraphQL schema on which the query is executed.
    /// </summary>
    ISchema Schema { get; }

    /// <summary>
    /// Gets the object type on which the field resolver is being executed.
    /// </summary>
    IObjectType ObjectType { get; }

    /// <summary>
    /// Gets the operation from the query that is being executed.
    /// </summary>
    IOperation Operation { get; }

    /// <summary>
    /// Gets the field selection for which a field resolver is
    /// being executed.
    /// </summary>
    ISelection Selection { get; }

    /// <summary>
    /// Gets access to the coerced variable values of the request.
    /// </summary>
    IVariableValueCollection Variables { get; }

    /// <summary>
    /// Gets the current execution path.
    /// </summary>
    Path Path { get; }

    /// <summary>
    /// Gets the previous (parent) resolver result.
    /// </summary>
    /// <typeparam name="T">
    /// The type to which the result shall be casted.
    /// </typeparam>
    /// <returns>
    /// Returns the previous (parent) resolver result.
    /// </returns>
    T Parent<T>();

    /// <summary>
    /// Gets a specific field argument value.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <typeparam name="T">
    /// The type to which the argument shall be casted to.
    /// </typeparam>
    /// <returns>
    /// Returns the value of the specified field argument as literal.
    /// </returns>
    T ArgumentValue<T>(string name);

    /// <summary>
    /// Gets a specific field argument as literal.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <typeparam name="TValueNode">
    /// The type to which the argument shall be casted to.
    /// </typeparam>
    /// <returns>
    /// Returns the value of the specified field argument as literal.
    /// </returns>
    TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode;

    /// <summary>
    /// Gets a specific field argument as optional.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <typeparam name="T">
    /// The type to which the argument shall be casted to.
    /// </typeparam>
    /// <returns>
    /// Returns the value of the specified field argument as optional.
    /// </returns>
    Optional<T> ArgumentOptional<T>(string name);

    /// <summary>
    /// Gets the value-kind of a specific field argument.
    /// </summary>
    /// <param name="name">
    /// The argument name.
    /// </param>
    /// <returns>
    /// Returns the value kind of the specified field argument kind.
    /// </returns>
    ValueKind ArgumentKind(string name);

    /// <summary>
    /// Gets as required service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">
    /// The service type.
    /// </typeparam>
    /// <returns>
    /// Returns the specified service.
    /// </returns>
    T Service<T>() where T : notnull;

    /// <summary>
    /// Gets as required service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">
    /// The service type.
    /// </typeparam>
    /// <returns>
    /// Returns the specified service.
    /// </returns>
    T Service<T>(object key) where T : notnull;

    /// <summary>
    /// Gets a resolver object containing one or more resolvers.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the resolver object.
    /// </typeparam>
    /// <returns>
    /// Returns a resolver object containing one or more resolvers.
    /// </returns>
    T Resolver<T>();

    /// <summary>
    /// Gets the resolver service provider.
    /// By default, the resolver service provider is scoped to the request,
    /// but middleware can create a resolver scope.
    /// </summary>
    IServiceProvider Services { get; set; }

    /// <summary>
    /// Gets the request scoped service provider.
    /// We preserve here the access to the original service provider of the request.
    /// </summary>
    IServiceProvider RequestServices { get; }

    /// <summary>
    /// Gets the name that the field will have in the response map.
    /// </summary>
    /// <value></value>
    string ResponseName { get; }

    /// <summary>
    /// Indicates that the context has errors. To report new errors use
    /// <see cref="ReportError(IError)"/>
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// The scoped context data dictionary can be used by middlewares and
    /// resolvers to store and retrieve data during execution scoped to the
    /// hierarchy.
    /// </summary>
    IImmutableDictionary<string, object?> ScopedContextData { get; set; }

    /// <summary>
    /// The local context data dictionary can be used by middlewares and
    /// resolvers to store and retrieve data during execution scoped to the
    /// field
    /// </summary>
    IImmutableDictionary<string, object?> LocalContextData { get; set; }

    /// <summary>
    /// Notifies when the connection underlying this request is aborted
    /// and thus request operations should be cancelled.
    /// </summary>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Gets as required service from the dependency injection container.
    /// </summary>
    /// <param name="service">The service type.</param>
    /// <returns>
    /// Returns the specified service.
    /// </returns>
    object Service(Type service);

    /// <summary>
    /// Report a non-terminating resolver error to the execution engine.
    /// The error will be displayed in the error section with a reference to
    /// the field selection that is associated with the current
    /// resolver context.
    /// </summary>
    /// <param name="errorMessage">
    /// The error message.
    /// </param>
    void ReportError(string errorMessage);

    /// <summary>
    /// Report a non-terminating resolver error to the execution engine.
    /// The error will be displayed in the error section with a reference to
    /// the field selection that is associated with the current
    /// resolver context.
    /// </summary>
    /// <param name="error">
    /// The error.
    /// </param>
    void ReportError(IError error);

    /// <summary>
    /// Report a non-terminating resolver error to the execution engine.
    /// The error will be displayed in the error section with a reference to
    /// the field selection that is associated with the current
    /// resolver context.
    /// </summary>
    /// <param name="exception">
    /// The exception that was thrown.
    /// </param>
    /// <param name="configure">
    /// A delegate to further configure the error object.
    /// </param>
    void ReportError(Exception exception, Action<IErrorBuilder>? configure = null);

    /// <summary>
    /// Gets the pre-compiled selections for the selection-set
    /// with the specified <paramref name="typeContext" />.
    /// type context.
    /// </summary>
    /// <param name="typeContext">
    /// The object type context.
    /// </param>
    /// <param name="selection">
    /// The selection for which the pre-compiled child selections shall be returned.
    /// </param>
    /// <param name="allowInternals">
    /// Include also internal selections that shall not be included into the result set.
    /// </param>
    /// <returns>
    /// Returns the pre-compiled selections for the <paramref name="selection" />
    /// with the specified <paramref name="typeContext" />.
    /// </returns>
    IReadOnlyList<ISelection> GetSelections(
        IObjectType typeContext,
        ISelection? selection = null,
        bool allowInternals = false);

    /// <summary>
    /// Selects the current field and returns a <see cref="ISelectionCollection"/> containing
    /// this selection.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="ISelectionCollection"/> containing
    /// the selections that match the given field name.
    /// </returns>
    ISelectionCollection Select();

    /// <summary>
    /// Selects all child fields that match the given field name and
    /// returns a <see cref="ISelectionCollection"/> containing
    /// these selections.
    /// </summary>
    /// <param name="fieldName">
    /// The field name to select.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ISelectionCollection"/> containing
    /// the selections that match the given field name.
    /// </returns>
    ISelectionCollection Select(string fieldName);

    /// <summary>
    /// Get the query root instance.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the query root.
    /// </typeparam>
    /// <returns>
    /// Returns the query root instance.
    /// </returns>
    T GetQueryRoot<T>();

    /// <summary>
    /// Clones the current resolver context.
    /// </summary>
    /// <returns>
    /// Returns the cloned resolver context.
    /// </returns>
    IResolverContext Clone();
}
