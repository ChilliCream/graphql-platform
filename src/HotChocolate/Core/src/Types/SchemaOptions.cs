using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate;

/// <summary>
/// Represents mutable schema options.
/// </summary>
public class SchemaOptions : IReadOnlySchemaOptions
{
    /// <summary>
    /// Gets or sets the name of the query type.
    /// </summary>
    public string? QueryTypeName { get; set; }

    /// <summary>
    /// Gets or sets the name of the mutation type.
    /// </summary>
    public string? MutationTypeName { get; set; }

    /// <summary>
    /// Gets or sets the name of the subscription type.
    /// </summary>
    public string? SubscriptionTypeName { get; set; }

    /// <summary>
    /// Defines if the schema allows the query type to be omitted.
    /// </summary>
    public bool StrictValidation { get; set; } = true;

    /// <summary>
    /// Defines if the CSharp XML documentation shall be integrated.
    /// </summary>
    public bool UseXmlDocumentation { get; set; } = true;

    /// <summary>
    /// A delegate which defines the name of the XML documentation file to be read.
    /// Only used if <seealso cref="UseXmlDocumentation"/> is true.
    /// </summary>
    public Func<Assembly, string>? ResolveXmlDocumentationFileName { get; set; }

    /// <summary>
    /// Defines if fields shall be sorted by name.
    /// Default: <c>false</c>
    /// </summary>
    public bool SortFieldsByName { get; set; }

    /// <summary>
    /// Defines if syntax nodes shall be preserved on the type system objects
    /// </summary>
    public bool PreserveSyntaxNodes { get; set; }

    /// <summary>
    /// Defines if types shall be removed from the schema that are
    /// unreachable from the root types.
    /// </summary>
    public bool RemoveUnreachableTypes { get; set; }

    /// <summary>
    /// Defines the default binding behavior.
    /// </summary>
    public BindingBehavior DefaultBindingBehavior { get; set; } =
        BindingBehavior.Implicit;

    /// <summary>
    /// Defines which members shall be by default inferred as GraphQL fields.
    /// This default applies to <see cref="ObjectType"/> and <see cref="ObjectTypeExtension"/>.
    /// </summary>
    public FieldBindingFlags DefaultFieldBindingFlags { get; set; } =
        FieldBindingFlags.Instance;

    /// <summary>
    /// Defines on which fields a middleware pipeline can be applied on.
    /// </summary>
    public FieldMiddlewareApplication FieldMiddleware { get; set; } =
        FieldMiddlewareApplication.UserDefinedFields;

    /// <summary>
    /// Defines if the experimental directive introspection feature shall be enabled.
    /// </summary>
    public bool EnableDirectiveIntrospection { get; set; }

    /// <summary>
    /// The default directive visibility when directive introspection is enabled.
    /// </summary>
    public DirectiveVisibility DefaultDirectiveVisibility { get; set; } =
        DirectiveVisibility.Public;

    /// <summary>
    /// Defines that the default resolver execution strategy.
    /// </summary>
    public ExecutionStrategy DefaultResolverStrategy { get; set; } =
        ExecutionStrategy.Parallel;

    /// <summary>
    /// Defines if the order of important middleware components shall be validated.
    /// </summary>
    public bool ValidatePipelineOrder { get; set; } = true;

    /// <summary>
    /// Defines if the runtime types of types shall be validated.
    /// </summary>
    public bool StrictRuntimeTypeValidation { get; set; }

    /// <summary>
    /// Defines a delegate that determines if a runtime
    /// is an instance of an <see cref="ObjectType{T}"/>.
    /// </summary>
    public IsOfTypeFallback? DefaultIsOfTypeCheck { get; set; }

    /// <summary>
    /// Defines if the OneOf spec RFC is enabled. This feature is experimental.
    /// </summary>
    public bool EnableOneOf { get; set; }

    /// <inheritdoc />
    public bool EnsureAllNodesCanBeResolved { get; set; }

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
    public bool EnableFlagEnums { get; set; }

    /// <summary>
    /// Enables the @defer directive.
    /// Defer and stream both are at the moment preview features.
    /// </summary>
    public bool EnableDefer { get; set; }

    /// <summary>
    /// Enables the @stream directive.
    /// Defer and stream both are at the moment preview features.
    /// </summary>
    public bool EnableStream { get; set; }

    /// <summary>
    /// Creates a mutable options object from a read-only options object.
    /// </summary>
    /// <param name="options">The read-only options object.</param>
    /// <returns>Returns a new mutable options object.</returns>
    public static SchemaOptions FromOptions(IReadOnlySchemaOptions options)
    {
        return new()
        {
            QueryTypeName = options.QueryTypeName,
            MutationTypeName = options.MutationTypeName,
            SubscriptionTypeName = options.SubscriptionTypeName,
            StrictValidation = options.StrictValidation,
            UseXmlDocumentation = options.UseXmlDocumentation,
            ResolveXmlDocumentationFileName = options.ResolveXmlDocumentationFileName,
            FieldMiddleware = options.FieldMiddleware,
            DefaultBindingBehavior = options.DefaultBindingBehavior,
            PreserveSyntaxNodes = options.PreserveSyntaxNodes,
            EnableDirectiveIntrospection = options.EnableDirectiveIntrospection,
            DefaultDirectiveVisibility = options.DefaultDirectiveVisibility,
            DefaultResolverStrategy = options.DefaultResolverStrategy,
            ValidatePipelineOrder = options.ValidatePipelineOrder,
            StrictRuntimeTypeValidation = options.StrictRuntimeTypeValidation,
            RemoveUnreachableTypes = options.RemoveUnreachableTypes,
            SortFieldsByName = options.SortFieldsByName,
            DefaultIsOfTypeCheck = options.DefaultIsOfTypeCheck,
            EnableOneOf = options.EnableOneOf,
            EnsureAllNodesCanBeResolved = options.EnsureAllNodesCanBeResolved,
            EnableFlagEnums = options.EnableFlagEnums,
            EnableDefer = options.EnableDefer,
            EnableStream = options.EnableStream,
            DefaultFieldBindingFlags = options.DefaultFieldBindingFlags
        };
    }
}
