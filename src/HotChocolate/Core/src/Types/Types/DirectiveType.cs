using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.Internal.FieldInitHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// A GraphQL schema describes directives which are used to annotate various parts of a
/// GraphQL document as an indicator that they should be evaluated differently by a
/// validator, executor, or client tool such as a code generator.
///
/// http://spec.graphql.org/draft/#sec-Type-System.Directives
/// </summary>
public class DirectiveType
    : TypeSystemObjectBase<DirectiveTypeDefinition>
    , IHasRuntimeType
{
    // see: http://spec.graphql.org/draft/#ExecutableDirectiveLocation
    private static readonly HashSet<DirectiveLocation> _executableLocations =
        new()
        {
            DirectiveLocation.Query,
            DirectiveLocation.Mutation,
            DirectiveLocation.Subscription,
            DirectiveLocation.Field,
            DirectiveLocation.FragmentDefinition,
            DirectiveLocation.FragmentSpread,
            DirectiveLocation.InlineFragment,
            DirectiveLocation.VariableDefinition
        };

    // see: http://spec.graphql.org/draft/#TypeSystemDirectiveLocation
    private static readonly HashSet<DirectiveLocation> _typeSystemLocations =
        new()
        {
            DirectiveLocation.Schema,
            DirectiveLocation.Scalar,
            DirectiveLocation.Object,
            DirectiveLocation.FieldDefinition,
            DirectiveLocation.ArgumentDefinition,
            DirectiveLocation.Interface,
            DirectiveLocation.Union,
            DirectiveLocation.Enum,
            DirectiveLocation.EnumValue,
            DirectiveLocation.InputObject,
            DirectiveLocation.InputFieldDefinition
        };

    private Action<IDirectiveTypeDescriptor>? _configure;
    private ITypeConverter _converter = default!;
    private InputParser _inputParser = default!;
    private InputFormatter _inputFormatter = default!;

    protected DirectiveType()
    {
        _configure = Configure;
    }

    public DirectiveType(Action<IDirectiveTypeDescriptor> configure)
    {
        _configure = configure ??
            throw new ArgumentNullException(nameof(configure));
    }

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
        => new() { Definition = definition };

    /// <summary>
    /// The associated syntax node from the GraphQL SDL.
    /// </summary>
    public DirectiveDefinitionNode? SyntaxNode { get; private set; }

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
    public ICollection<DirectiveLocation> Locations { get; private set; } = default!;

    /// <summary>
    /// Gets the directive arguments.
    /// </summary>
    public FieldCollection<Argument> Arguments { get; private set; } = default!;

    public IReadOnlyList<DirectiveMiddleware> MiddlewareComponents { get; private set; } =
        default!;

    public bool HasMiddleware { get; private set; }

    /// <summary>
    /// Defines that this directive can be used in executable GraphQL documents.
    ///
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
    /// </summary>
    public bool IsExecutableDirective { get; private set; }

    /// <summary>
    /// Defines that this directive can be applied to type system members.
    ///
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
    /// </summary>
    public bool IsTypeSystemDirective { get; private set; }

    /// <summary>
    /// Defines if instances of this directive type are publicly visible through introspection.
    /// </summary>
    internal bool IsPublic { get; private set; }

    protected override DirectiveTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
    {
        try
        {
            if (Definition is null)
            {
                var descriptor = DirectiveTypeDescriptor.FromSchemaType(
                    context.DescriptorContext,
                    GetType());
                _configure!(descriptor);
                return descriptor.CreateDefinition();
            }

            return Definition;
        }
        finally
        {
            _configure = null;
        }
    }

    protected virtual void Configure(IDirectiveTypeDescriptor descriptor) { }

    protected override void OnRegisterDependencies(
        ITypeDiscoveryContext context,
        DirectiveTypeDefinition definition)
    {
        base.OnRegisterDependencies(context, definition);

        RuntimeType = definition.RuntimeType != GetType()
            ? definition.RuntimeType
            : typeof(object);
        IsRepeatable = definition.IsRepeatable;

        TypeDependencyHelper.CollectDependencies(definition, context.Dependencies);
    }

    protected override void OnCompleteType(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        base.OnCompleteType(context, definition);

        _converter = context.Services.GetTypeConverter();
        _inputFormatter = context.DescriptorContext.InputFormatter;
        _inputParser = context.DescriptorContext.InputParser;
        MiddlewareComponents = definition.GetMiddlewareComponents();

        SyntaxNode = definition.SyntaxNode;
        Locations = definition.GetLocations().ToList().AsReadOnly();
        Arguments = OnCompleteFields(context, definition);
        HasMiddleware = MiddlewareComponents.Count > 0;
        IsPublic = definition.IsPublic;

        if (Locations.Count == 0)
        {
            context.ReportError(SchemaErrorBuilder.New()
                .SetMessage(string.Format(
                    CultureInfo.InvariantCulture,
                    TypeResources.DirectiveType_NoLocations,
                    Name))
                .SetCode(ErrorCodes.Schema.MissingType)
                .SetTypeSystemObject(context.Type)
                .AddSyntaxNode(definition.SyntaxNode)
                .Build());
        }

        IsExecutableDirective = _executableLocations.Overlaps(Locations);
        IsTypeSystemDirective = _typeSystemLocations.Overlaps(Locations);
    }

    protected virtual FieldCollection<Argument> OnCompleteFields(
        ITypeCompletionContext context,
        DirectiveTypeDefinition definition)
    {
        return CompleteFields(context, this, definition.GetArguments(), CreateArgument);
        static Argument CreateArgument(DirectiveArgumentDefinition argDef, int index)
            => new(argDef, index);
    }

    internal IValueNode SerializeArgument(Argument argument, object? obj)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(nameof(argument));
        }

        var path = PathFactory.Instance.New(argument.Name);
        return _inputFormatter.FormatValue(obj, argument.Type, path);
    }

    internal object? DeserializeArgument(Argument argument, IValueNode literal, Type target)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(nameof(argument));
        }

        if (literal is null)
        {
            throw new ArgumentNullException(nameof(literal));
        }

        var obj = _inputParser.ParseLiteral(literal, argument);

        if (target.IsInstanceOfType(obj))
        {
            return obj;
        }

        if (_converter.TryConvert(typeof(object), target, obj, out var o))
        {
            return o;
        }

        throw new ArgumentException(
            TypeResources.DirectiveType_UnableToConvert,
            nameof(target));
    }

    internal T DeserializeArgument<T>(Argument argument, IValueNode literal)
        => (T)DeserializeArgument(argument, literal, typeof(T))!;
}
