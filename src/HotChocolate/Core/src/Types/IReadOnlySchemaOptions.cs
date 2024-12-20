using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

/// <summary>
/// Represents read-only schema options.
/// </summary>
public interface IReadOnlySchemaOptions
{
    /// <summary>
    /// Gets or sets the name of the query type.
    /// </summary>
    string? QueryTypeName { get; }

    /// <summary>
    /// Gets or sets the name of the mutation type.
    /// </summary>
    string? MutationTypeName { get; }

    /// <summary>
    /// Gets or sets the name of the subscription type.
    /// </summary>
    string? SubscriptionTypeName { get; }

    /// <summary>
    /// Defines if the schema allows the query type to be omitted.
    /// </summary>
    bool StrictValidation { get; }

    /// <summary>
    /// Defines if the CSharp XML documentation shall be integrated.
    /// </summary>
    bool UseXmlDocumentation { get; }

    /// <summary>
    /// A delegate which defines the name of the XML documentation file to be read.
    /// Only used if <seealso cref="UseXmlDocumentation"/> is true.
    /// </summary>
    Func<Assembly, string>? ResolveXmlDocumentationFileName { get; }

    /// <summary>
    /// Defines if fields shall be sorted by name.
    /// Default: <c>false</c>
    /// </summary>
    bool SortFieldsByName { get; }

    /// <summary>
    /// Defines if types shall be removed from the schema that are
    /// unreachable from the root types.
    /// </summary>
    bool RemoveUnreachableTypes { get; }

    /// <summary>
    /// Defines if unused type system directives shall
    /// be removed from the schema.
    /// </summary>
    bool RemoveUnusedTypeSystemDirectives { get; }

    /// <summary>
    /// Defines the default binding behavior.
    /// </summary>
    BindingBehavior DefaultBindingBehavior { get; }

    /// <summary>
    /// Defines which members shall be by default inferred as GraphQL fields.
    /// This default applies to <see cref="ObjectType"/> and <see cref="ObjectTypeExtension"/>.
    /// </summary>
    FieldBindingFlags DefaultFieldBindingFlags { get; }

    /// <summary>
    /// Defines on which fields a middleware pipeline can be applied on.
    /// </summary>
    FieldMiddlewareApplication FieldMiddleware { get; }

    /// <summary>
    /// Defines if the experimental directive introspection feature shall be enabled.
    /// </summary>
    bool EnableDirectiveIntrospection { get; }

    /// <summary>
    /// The default directive visibility when directive introspection is enabled.
    /// </summary>
    DirectiveVisibility DefaultDirectiveVisibility { get; }

    /// <summary>
    /// Defines that the default resolver execution strategy.
    /// </summary>
    ExecutionStrategy DefaultResolverStrategy { get; }

    /// <summary>
    /// Defines if the order of important middleware components shall be validated.
    /// </summary>
    bool ValidatePipelineOrder { get; }

    /// <summary>
    /// Defines if the runtime types of types shall be validated.
    /// </summary>
    bool StrictRuntimeTypeValidation { get; }

    /// <summary>
    /// Defines a delegate that determines if a runtime
    /// is an instance of an <see cref="ObjectType{T}"/>.
    /// </summary>
    IsOfTypeFallback? DefaultIsOfTypeCheck { get; }

    /// <summary>
    /// Defines if the OneOf spec RFC is enabled. This feature is experimental.
    /// </summary>
    bool EnableOneOf { get; }

    /// <summary>
    /// Defines if the schema building process shall validate that all nodes are resolvable through `node`.
    /// </summary>
    bool EnsureAllNodesCanBeResolved { get; }

    /// <summary>
    /// Defines if flag enums should be inferred as object value nodes
    /// </summary>
    /// <example>
    /// Given the following enum
    /// <br/>
    /// <code>
    /// [Flags]
    /// public enum Example { First, Second, Third }
    ///
    /// public class Query { public Example Loopback(Example input) => input;
    /// </code>
    /// <br/>
    /// The following schema is produced
    /// <br/>
    /// <code>
    /// type Query {
    ///    loopback(input: ExampleFlagsInput!): ExampleFlags
    /// }
    ///
    /// type ExampleFlags {
    ///    isFirst: Boolean!
    ///    isSecond: Boolean!
    ///    isThird: Boolean!
    /// }
    ///
    /// input ExampleFlagsInput {
    ///    isFirst: Boolean
    ///    isSecond: Boolean
    ///    isThird: Boolean
    /// }
    /// </code>
    /// </example>
    bool EnableFlagEnums { get; }

    /// <summary>
    /// Enables the @defer directive.
    /// Defer and stream both are at the moment preview features.
    /// </summary>
    bool EnableDefer { get; }

    /// <summary>
    /// Enables the @stream directive.
    /// Defer and stream both are at the moment preview features.
    /// </summary>
    bool EnableStream { get; }

    /// <summary>
    /// Enables the @semanticNonNull directive and rewrites Non-Null types to nullable types
    /// with this directive attached to indicate semantic non-nullability.
    /// This feature is experimental and might be changed or removed in the future.
    /// </summary>
    bool EnableSemanticNonNull { get; }

    /// <summary>
    /// Specifies the maximum allowed nodes that can be fetched at once through the nodes field.
    /// </summary>
    int MaxAllowedNodeBatchSize { get; }

    /// <summary>
    /// Specified if the leading I shall be stripped from the interface name.
    /// </summary>
    bool StripLeadingIFromInterface { get; }

    /// <summary>
    /// Specifies that the @tag directive shall be registered with the type system.
    /// </summary>
    bool EnableTag { get; }

    /// <summary>
    /// Errors if either an ASP.NET Core [Authorize] or [AllowAnonymous] attribute
    /// is used on a Hot Chocolate resolver or type definition.
    /// </summary>
    bool ErrorOnAspNetCoreAuthorizationAttributes { get; }

    /// <summary>
    /// Specifies the default dependency injection scope for query fields.
    /// </summary>
    public DependencyInjectionScope DefaultQueryDependencyInjectionScope { get; }

    /// <summary>
    /// Specifies the default dependency injection scope for mutation fields.
    /// </summary>
    public DependencyInjectionScope DefaultMutationDependencyInjectionScope { get; }

    /// <summary>
    /// Specifies if the elements of paginated root fields should be published
    /// to the DataLOader promise cache.
    /// </summary>
    bool PublishRootFieldPagesToPromiseCache { get; }
}
