using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Resolvers;

/// <summary>
/// The resolver context represent the execution context for a specific
/// field that is being resolved.
/// </summary>
public interface IResolverContext : IPureResolverContext
{
    /// <summary>
    /// Gets the resolver service provider.
    /// By default the resolver service provider is scoped to the request,
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
    new IImmutableDictionary<string, object?> ScopedContextData { get; set; }

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
    /// The parser used for parsing input values.
    /// </summary>
    internal InputParser Parser { get; }

    /// <summary>
    /// The <see cref="ITypeConverter"/> used to convert between types.
    /// </summary>
    internal ITypeConverter Converter { get; }

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
