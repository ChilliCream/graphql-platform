using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HotChocolate.Language;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Resolvers
{
    /// <summary>
    /// The resolver context represent the execution context for a specific
    /// field that is being resolved.
    /// </summary>
    public interface IResolverContext : IPureResolverContext
    {
        /// <summary>
        /// Gets the scoped request service provider.
        /// </summary>
        IServiceProvider Services { get; set; }

        /// <summary>
        /// Gets the object type on which the field resolver is being executed.
        /// </summary>
        IObjectType ObjectType { get; }

        /// <summary>
        /// Gets the field on which the field resolver is being executed.
        /// </summary>
        [Obsolete("Use Selection.Field or Selection.Type.")]
        IObjectField Field { get; }

        /// <summary>
        /// Gets the parsed query document that is being executed.
        /// </summary>
        DocumentNode Document { get; }

        /// <summary>
        /// Gets the operation from the query that is being executed.
        /// </summary>
        OperationDefinitionNode Operation { get; }

        /// <summary>
        /// Gets the merged field selection for which a field resolver is
        /// being executed.
        /// </summary>
        [Obsolete("Use Selection.SyntaxNode")]
        FieldNode FieldSelection { get; }

        /// <summary>
        /// Gets the name that the field will have in the response map.
        /// </summary>
        /// <value></value>
        NameString ResponseName { get; }

        /// <summary>
        /// Gets the current execution path.
        /// </summary>
        Path Path { get; }

        /// <summary>
        /// Indicates that the context has errors. To report new errors use <see cref="ReportError(IError)"/>
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// The scoped context data dictionary can be used by middlewares and
        /// resolvers to store and retrieve data during execution scoped to the
        /// hierarchy
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
        /// Gets a specific field argument.
        /// </summary>
        /// <param name="name">
        /// The argument name.
        /// </param>
        /// <typeparam name="T">
        /// The type to which the argument shall be casted to.
        /// </typeparam>
        /// <returns>
        /// Returns the value of the specified field argument.
        /// </returns>
        [Obsolete("Use ArgumentValue<T>(name) or " +
                  "ArgumentLiteral<TValueNode>(name) or " +
                  "ArgumentOptional<T>(name).")]
        [return: MaybeNull]
        T Argument<T>(NameString name);

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
        /// The error will be displayed in the errorsection with a reference to
        /// the field selection that is associated with the current
        /// resolver context.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        void ReportError(string errorMessage);

        /// <summary>
        /// Report a non-terminating resolver error to the execution engine.
        /// The error will be displayed in the errorsection with a reference to
        /// the field selection that is associated with the current
        /// resolver context.
        /// </summary>
        /// <param name="error">
        /// The error.
        /// </param>
        void ReportError(IError error);

        /// <summary>
        /// Gets the pre-compiled selections for the <paramref name="selectionSet" />
        /// with the specified <paramref name="typeContext" />.
        /// type context.
        /// </summary>
        /// <param name="typeContext">
        /// The object type context.
        /// </param>
        /// <param name="selectionSet">
        /// The selection-set for which the pre-compiled selections shall be returned.
        /// </param>
        /// <param name="allowInternals">
        /// Include also internal selections that shall not be included into the result set.
        /// </param>
        /// <returns>
        /// Returns the pre-compiled selections for the <paramref name="selectionSet" />
        /// with the specified <paramref name="typeContext" />.
        /// </returns>
        IReadOnlyList<IFieldSelection> GetSelections(
            ObjectType typeContext,
            SelectionSetNode? selectionSet = null,
            bool allowInternals = false);

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
    }
}
