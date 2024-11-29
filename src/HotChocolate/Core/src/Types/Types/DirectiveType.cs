#nullable enable

using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// A GraphQL schema describes directives which are used to annotate various parts of a
/// GraphQL document as an indicator that they should be evaluated differently by a
/// validator, executor, or client tool such as a code generator.
/// </para>
/// <para>https://spec.graphql.org/draft/#sec-Type-System.Directives</para>
/// </summary>
public partial class DirectiveType
    : TypeSystemObjectBase<DirectiveTypeDefinition>
    , IHasRuntimeType
    , IHasTypeIdentity
{
    private Action<IDirectiveTypeDescriptor>? _configure;
    private Func<object?[], object> _createInstance = default!;
    private Action<object, object?[]> _getFieldValues = default!;
    private Func<DirectiveNode, object> _parse = default!;
    private Func<object, DirectiveNode> _format = default!;
    private InputParser _inputParser = default!;

    protected DirectiveType()
        => _configure = Configure;

    public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
        => _configure = configure ?? throw new ArgumentNullException(nameof(configure));

    /// <summary>
    /// Create a directive type from a type definition.
    /// </summary>
    /// <param name="definition">
    /// The directive type definition that specifies the properties of the
    /// newly created directive type.
    /// </param>
    /// <returns>
    /// Returns the newly created directive type.
    /// </returns>
    public static DirectiveType CreateUnsafe(DirectiveTypeDefinition definition)
        => new() { Definition = definition, };

    /// <summary>
    /// Gets the runtime type.
    /// The runtime type defines of which value the type is when it
    /// manifests in the execution engine.
    /// </summary>
    public Type RuntimeType { get; private set; } = default!;

    /// <summary>
    /// Defines if this directive is repeatable. Repeatable directives are often useful when
    /// the same directive should be used with different arguments at a single location,
    /// especially in cases where additional information needs to be provided to a type or
    /// schema extension via a directive
    /// </summary>
    public bool IsRepeatable { get; private set; }

    /// <summary>
    /// Gets the locations where this directive type can be used to annotate
    /// a type system member.
    /// </summary>
    public DirectiveLocation Locations { get; private set; }

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    public FieldCollection<DirectiveArgument> Arguments { get; private set; } = default!;

    /// <summary>
    /// Gets the directive field middleware.
    /// </summary>
    public DirectiveMiddleware? Middleware { get; private set; }

    /// <summary>
    /// <para>Defines that this directive can be used in executable GraphQL documents.</para>
    /// <para>
    /// In order to be executable a directive must at least be valid
    /// in one of the following locations:
    /// QUERY (<see cref="DirectiveLocation.Query"/>)
    /// MUTATION (<see cref="DirectiveLocation.Mutation"/>)
    /// SUBSCRIPTION (<see cref="DirectiveLocation.Subscription"/>)
    /// FIELD (<see cref="DirectiveLocation.Field"/>)
    /// FRAGMENT_DEFINITION (<see cref="DirectiveLocation.FragmentDefinition"/>)
    /// FRAGMENT_SPREAD (<see cref="DirectiveLocation.FragmentSpread"/>)
    /// INLINE_FRAGMENT (<see cref="DirectiveLocation.InlineFragment"/>)
    /// VARIABLE_DEFINITION (<see cref="DirectiveLocation.VariableDefinition"/>)
    /// </para>
    /// </summary>
    public bool IsExecutableDirective { get; private set; }

    /// <summary>
    /// <para>Defines that this directive can be applied to type system members.</para>
    /// <para>
    /// In order to be a type system directive it must at least be valid
    /// in one of the following locations:
    /// SCHEMA (<see cref="DirectiveLocation.Schema"/>)
    /// SCALAR (<see cref="DirectiveLocation.Scalar"/>)
    /// OBJECT (<see cref="DirectiveLocation.Object"/>)
    /// FIELD_DEFINITION (<see cref="DirectiveLocation.FieldDefinition"/>)
    /// ARGUMENT_DEFINITION (<see cref="DirectiveLocation.ArgumentDefinition"/>)
    /// INTERFACE (<see cref="DirectiveLocation.Interface"/>)
    /// UNION (<see cref="DirectiveLocation.Union"/>)
    /// ENUM (<see cref="DirectiveLocation.Enum"/>)
    /// ENUM_VALUE (<see cref="DirectiveLocation.EnumValue"/>)
    /// INPUT_OBJECT (<see cref="DirectiveLocation.InputObject"/>)
    /// INPUT_FIELD_DEFINITION (<see cref="DirectiveLocation.InputFieldDefinition"/>)
    /// </para>
    /// </summary>
    public bool IsTypeSystemDirective { get; private set; }

    /// <summary>
    /// Defines if instances of this directive type are publicly visible through introspection.
    /// </summary>
    internal bool IsPublic { get; private set; }

    internal object CreateInstance(object?[] fieldValues)
        => _createInstance(fieldValues);

    internal void GetFieldValues(object runtimeValue, object?[] fieldValues)
        => _getFieldValues(runtimeValue, fieldValues);

    public object Parse(DirectiveNode directiveNode)
        => _parse(directiveNode);

    public DirectiveNode Format(object runtimeValue)
        => _format(runtimeValue);

    public T ParseArgument<T>(string name, IValueNode? value)
    {
        name.EnsureGraphQLName();

        if (!Arguments.TryGetField(name, out var argument))
        {
            throw new ArgumentException(
                string.Format(
                    Directive_GetArgumentValue_UnknownArgument,
                    Name,
                    name));
        }

        value ??= argument.DefaultValue ?? NullValueNode.Default;

        return (T)_inputParser.ParseLiteral(value, argument, typeof(T))!;
    }

    private Type? TypeIdentity { get; set; }

    Type? IHasTypeIdentity.TypeIdentity => TypeIdentity;
}
