using System.Collections.Immutable;
using HotChocolate;
using HotChocolate.Validation.Options;
using HotChocolate.Validation.Rules;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IValidationBuilder"/>
/// </summary>
public static partial class HotChocolateValidationBuilderExtensions
{
    /// <summary>
    /// Every argument provided to a field or directive must be defined
    /// in the set of possible arguments of that field or directive.
    ///
    /// https://spec.graphql.org/June2018/#sec-Argument-Names
    ///
    /// AND
    ///
    /// Fields and directives treat arguments as a mapping of argument name
    /// to value.
    ///
    /// More than one argument with the same name in an argument set
    /// is ambiguous and invalid.
    ///
    /// https://spec.graphql.org/June2018/#sec-Argument-Uniqueness
    ///
    /// AND
    ///
    /// Arguments can be required. An argument is required if the argument
    /// type is non‐null and does not have a default value. Otherwise,
    /// the argument is optional.
    ///
    /// https://spec.graphql.org/June2018/#sec-Required-Arguments
    /// </summary>
    public static IValidationBuilder AddArgumentRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<ArgumentVisitor>();
    }

    /// <summary>
    /// GraphQL servers define what directives they support.
    /// For each usage of a directive, the directive must be available
    /// on that server.
    ///
    /// https://spec.graphql.org/June2018/#sec-Directives-Are-Defined
    ///
    /// AND
    ///
    /// GraphQL servers define what directives they support and where they
    /// support them.
    ///
    /// For each usage of a directive, the directive must be used in a
    /// location that the server has declared support for.
    ///
    /// https://spec.graphql.org/June2018/#sec-Directives-Are-In-Valid-Locations
    ///
    /// AND
    ///
    /// Directives are used to describe some metadata or behavioral change on
    /// the definition they apply to.
    ///
    /// When more than one directive of the
    /// same name is used, the expected metadata or behavior becomes ambiguous,
    /// therefore only one of each directive is allowed per location.
    ///
    /// https://spec.graphql.org/draft/#sec-Directives-Are-Unique-Per-Location
    /// </summary>
    public static IValidationBuilder AddDirectiveRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<DirectiveVisitor>();
    }

    /// <summary>
    /// GraphQL execution will only consider the executable definitions
    /// Operation and Fragment.
    ///
    /// Type system definitions and extensions are not executable,
    /// and are not considered during execution.
    ///
    /// To avoid ambiguity, a document containing TypeSystemDefinition
    /// is invalid for execution.
    ///
    /// GraphQL documents not intended to be directly executed may
    /// include TypeSystemDefinition.
    ///
    /// https://spec.graphql.org/June2018/#sec-Executable-Definitions
    /// </summary>
    public static IValidationBuilder AddDocumentRules(
        this IValidationBuilder builder)
    {
        return builder.ConfigureValidation(
            m => m.RulesModifiers.Add((_, r) =>
            {
                if (r.Rules.All(t => t.GetType() != typeof(DocumentRule)))
                {
                    r.Rules.Add(new DocumentRule());
                }
            }));
    }

    /// <summary>
    /// The target field of a field selection must be defined on the scoped
    /// type of the selection set. There are no limitations on alias names.
    ///
    /// https://spec.graphql.org/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
    ///
    /// AND
    ///
    /// Field selections on scalars or enums are never allowed,
    /// because they are the leaf nodes of any GraphQL query.
    ///
    /// Conversely, the leaf field selections of GraphQL queries
    /// must be of type scalar or enum. Leaf selections on objects,
    /// interfaces, and unions without subfields are disallowed.
    ///
    /// https://spec.graphql.org/June2018/#sec-Leaf-Field-Selections
    /// </summary>
    public static IValidationBuilder AddFieldRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<FieldVisitor>();
    }

    /// <summary>
    /// Fragment definitions are referenced in fragment spreads by name.
    /// To avoid ambiguity, each fragment’s name must be unique within a
    /// document.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragment-Name-Uniqueness
    ///
    /// AND
    ///
    /// Defined fragments must be used within a document.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragments-Must-Be-Used
    ///
    /// AND
    ///
    /// Fragments can only be declared on unions, interfaces, and objects.
    /// They are invalid on scalars.
    /// They can only be applied on non‐leaf fields.
    /// This rule applies to both inline and named fragments.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragments-On-Composite-Types
    ///
    /// AND
    ///
    /// Fragments are declared on a type and will only apply when the
    /// runtime object type matches the type condition.
    ///
    /// They also are spread within the context of a parent type.
    ///
    /// A fragment spread is only valid if its type condition could ever
    /// apply within the parent type.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragment-spread-is-possible
    ///
    /// AND
    ///
    /// Named fragment spreads must refer to fragments defined within the
    /// document.
    ///
    /// It is a validation error if the target of a spread is not defined.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragment-spread-target-defined
    ///
    /// AND
    ///
    /// The graph of fragment spreads must not form any cycles including
    /// spreading itself. Otherwise, an operation could infinitely spread or
    /// infinitely execute on cycles in the underlying data.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragment-spreads-must-not-form-cycles
    ///
    /// AND
    ///
    /// Fragments must be specified on types that exist in the schema.
    /// This applies for both named and inline fragments.
    /// If they are not defined in the schema, the query does not validate.
    ///
    /// https://spec.graphql.org/June2018/#sec-Fragment-Spread-Type-Existence
    /// </summary>
    public static IValidationBuilder AddFragmentRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<FragmentVisitor>();
    }

    /// <summary>
    /// Every input field provided in an input object value must be defined in
    /// the set of possible fields of that input object’s expected type.
    ///
    /// https://spec.graphql.org/June2018/#sec-Input-Object-Field-Names
    ///
    /// AND
    ///
    /// Input objects must not contain more than one field of the same name,
    /// otherwise an ambiguity would exist which includes an ignored portion
    /// of syntax.
    ///
    /// https://spec.graphql.org/June2018/#sec-Input-Object-Field-Uniqueness
    ///
    /// AND
    ///
    /// Input object fields may be required. Much like a field may have
    /// required arguments, an input object may have required fields.
    ///
    /// An input field is required if it has a non‐null type and does not have
    /// a default value. Otherwise, the input object field is optional.
    ///
    /// https://spec.graphql.org/June2018/#sec-Input-Object-Required-Fields
    ///
    /// AND
    ///
    /// Literal values must be compatible with the type expected in the position
    /// they are found as per the coercion rules defined in the Type System
    /// chapter.
    ///
    /// https://spec.graphql.org/June2018/#sec-Values-of-Correct-Type
    /// </summary>
    public static IValidationBuilder AddValueRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<ValueVisitor>();
    }

    /// <summary>
    /// If any operation defines more than one variable with the same name,
    /// it is ambiguous and invalid. It is invalid even if the type of the
    /// duplicate variable is the same.
    ///
    /// https://spec.graphql.org/June2018/#sec-Validation.Variables
    ///
    /// AND
    ///
    /// Variables can only be input types. Objects,
    /// unions, and interfaces cannot be used as inputs.
    ///
    /// https://spec.graphql.org/June2018/#sec-Variables-Are-Input-Types
    ///
    /// AND
    ///
    /// All variables defined by an operation must be used in that operation
    /// or a fragment transitively included by that operation.
    ///
    /// Unused variables cause a validation error.
    ///
    /// https://spec.graphql.org/June2018/#sec-All-Variables-Used
    ///
    /// AND
    ///
    /// Variables are scoped on a per‐operation basis. That means that
    /// any variable used within the context of an operation must be defined
    /// at the top level of that operation
    ///
    /// https://spec.graphql.org/June2018/#sec-All-Variable-Uses-Defined
    ///
    /// AND
    ///
    /// Variable usages must be compatible with the arguments
    /// they are passed to.
    ///
    /// Validation failures occur when variables are used in the context
    /// of types that are complete mismatches, or if a nullable type in a
    ///  variable is passed to a non‐null argument type.
    ///
    /// https://spec.graphql.org/June2018/#sec-All-Variable-Usages-are-Allowed
    /// </summary>
    public static IValidationBuilder AddVariableRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<VariableVisitor>();
    }

    /// <summary>
    /// GraphQL allows a short‐hand form for defining query operations
    /// when only that one operation exists in the document.
    ///
    /// https://spec.graphql.org/June2018/#sec-Lone-Anonymous-Operation
    ///
    /// AND
    ///
    /// Each named operation definition must be unique within a document
    /// when referred to by its name.
    ///
    /// https://spec.graphql.org/June2018/#sec-Operation-Name-Uniqueness
    ///
    /// AND
    ///
    /// Subscription operations must have exactly one root field.
    ///
    /// https://spec.graphql.org/June2018/#sec-Single-root-field
    /// </summary>
    public static IValidationBuilder AddOperationRules(
        this IValidationBuilder builder)
    {
        return builder.TryAddValidationVisitor<OperationVisitor>();
    }

    /// <summary>
    /// Adds a validation rule that restricts the depth of a GraphQL request.
    /// </summary>
    /// <param name="builder">
    /// The validation builder.
    /// </param>
    /// <param name="maxAllowedExecutionDepth">
    /// The max allowed GraphQL request depth.
    /// </param>
    /// <param name="skipIntrospectionFields">
    /// Specifies if depth analysis is skipped for introspection queries.
    /// </param>
    /// <param name="allowRequestOverrides">
    /// Defines if request depth overrides are allowed on a per-request basis.
    /// </param>
    /// <param name="isEnabled">
    /// A delegate that defines if the rule is enabled.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IValidationBuilder"/> for configuration chaining.
    /// </returns>
    public static IValidationBuilder AddMaxExecutionDepthRule(
        this IValidationBuilder builder,
        int maxAllowedExecutionDepth,
        bool skipIntrospectionFields = false,
        bool allowRequestOverrides = false,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
    {
        return builder
            .TryAddValidationVisitor(
                (_, o) => new MaxExecutionDepthVisitor(o),
                priority: 2,
                isCacheable: !allowRequestOverrides,
                isEnabled: isEnabled)
            .ModifyValidationOptions(o =>
            {
                o.MaxAllowedExecutionDepth = maxAllowedExecutionDepth;
                o.SkipIntrospectionFields = skipIntrospectionFields;
            });
    }

    /// <summary>
    /// Adds a validation rule that only allows requests to use `__schema` or `__type`
    /// if the request carries an introspection allowed flag.
    /// </summary>
    public static IValidationBuilder AddIntrospectionAllowedRule(
        this IValidationBuilder builder)
        => builder.TryAddValidationVisitor(
            (_, _) => new IntrospectionVisitor(),
            priority: 0,
            isCacheable: false,
            isEnabled: (_, o) => o.DisableIntrospection);

    /// <summary>
    /// Adds a validation rule that restricts the depth of a GraphQL introspection request.
    /// </summary>
    public static IValidationBuilder AddIntrospectionDepthRule(
        this IValidationBuilder builder)
        => builder.TryAddValidationVisitor<IntrospectionDepthVisitor>(
            priority: 1,
            factory: (_, o) => new IntrospectionDepthVisitor(o),
            isEnabled: (_, o) => !o.DisableDepthRule);

    /// <summary>
    /// Adds a validation rule that restricts the depth of coordinate cycles in GraphQL operations.
    /// </summary>
    public static IValidationBuilder AddMaxAllowedFieldCycleDepthRule(
        this IValidationBuilder builder,
        ushort? defaultCycleLimit = 3,
        (SchemaCoordinate Coordinate, ushort MaxAllowed)[]? coordinateCycleLimits = null,
        Func<IServiceProvider, ValidationOptions, bool>? isEnabled = null)
        => builder.TryAddValidationVisitor(
            (_, _) => new MaxAllowedFieldCycleDepthVisitor(
                coordinateCycleLimits?.ToImmutableArray()
                    ?? ImmutableArray<(SchemaCoordinate, ushort)>.Empty,
                defaultCycleLimit),
            priority: 3,
            isEnabled: isEnabled);

    /// <summary>
    /// Removes a validation rule that restricts the depth of coordinate cycles in GraphQL operations.
    /// </summary>
    public static IValidationBuilder RemoveMaxAllowedFieldCycleDepthRule(
        this IValidationBuilder builder)
        => builder.TryRemoveValidationVisitor<MaxAllowedFieldCycleDepthVisitor>();
}
