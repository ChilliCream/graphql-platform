using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Types.ThrowHelper;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL output field definition in a fusion schema.
/// </summary>
public sealed class FusionOutputFieldDefinition : IOutputFieldDefinition, IInaccessibleProvider
{
    private bool _completed;
    private readonly FieldDefinitionFlags _flags;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionOutputFieldDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="description">The description of the field.</param>
    /// <param name="deprecationReason">
    /// The deprecation reason, or <c>null</c> if the field is not deprecated.
    /// An empty or white-space value is treated as <c>null</c>.
    /// </param>
    /// <param name="isInaccessible">A value indicating whether the field is marked as inaccessible.</param>
    /// <param name="isGatewayField">A value indicating whether the field is implemented by the gateway rather than resolved from a source schema.</param>
    /// <param name="arguments">The collection of arguments for this field.</param>
    public FusionOutputFieldDefinition(
        string name,
        string? description,
        string? deprecationReason,
        bool isInaccessible,
        bool isGatewayField,
        FusionInputFieldDefinitionCollection arguments)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(arguments);

        Name = name;
        Description = description;
        DeprecationReason = string.IsNullOrWhiteSpace(deprecationReason) ? null : deprecationReason;
        Arguments = arguments;

        var flags = FieldDefinitionFlags.None;

        if (name.StartsWith("__"))
        {
            flags |= FieldDefinitionFlags.Introspection;
        }

        if (isInaccessible)
        {
            flags |= FieldDefinitionFlags.Inaccessible;
        }

        if (isGatewayField)
        {
            flags |= FieldDefinitionFlags.GatewayField;
        }

        _flags = flags;

        // these properties are initialized
        // in the type complete step.
        Type = null!;
        Sources = null!;
        DeclaringType = null!;
        Directives = null!;
        Features = null!;
    }

    /// <summary>
    /// Gets the name of this field.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this field.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the complex type that declares this field.
    /// </summary>
    public FusionComplexTypeDefinition DeclaringType
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IComplexTypeDefinition IOutputFieldDefinition.DeclaringType => DeclaringType;

    ITypeSystemMember IFieldDefinition.DeclaringMember => DeclaringType;

    /// <summary>
    /// Gets the schema coordinate of this field.
    /// </summary>
    public SchemaCoordinate Coordinate => new(DeclaringType.Name, Name, ofDirective: false);

    /// <summary>
    /// Defines if this field is deprecated.
    /// This is <c>true</c> if a <see cref="DeprecationReason"/> is present.
    /// </summary>
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    public bool IsDeprecated => DeprecationReason is not null;

    /// <summary>
    /// Gets a value indicating whether this field is an introspection field.
    /// </summary>
    public bool IsIntrospectionField => (_flags & FieldDefinitionFlags.Introspection) == FieldDefinitionFlags.Introspection;

    /// <summary>
    /// Gets the deprecation reason, or <c>null</c> if this field is not deprecated.
    /// </summary>
    public string? DeprecationReason { get; }

    /// <summary>
    /// Gets a value indicating whether this field is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible => (_flags & FieldDefinitionFlags.Inaccessible) == FieldDefinitionFlags.Inaccessible;

    /// <summary>
    /// Gets a value indicating whether this field is implemented by the gateway rather than
    /// resolved from a source schema.
    /// </summary>
    public bool IsGatewayField => (_flags & FieldDefinitionFlags.GatewayField) == FieldDefinitionFlags.GatewayField;

    /// <summary>
    /// Gets the directives applied to this field.
    /// </summary>
    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives;

    /// <summary>
    /// Gets the collection of arguments for this field.
    /// </summary>
    public FusionInputFieldDefinitionCollection Arguments { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> IOutputFieldDefinition.Arguments
        => Arguments;

    /// <summary>
    /// Gets the output type of this field.
    /// </summary>
    public IOutputType Type
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Gets the field flags.
    /// </summary>
    public FieldFlags Flags => FieldFlags.None;

    IType IFieldDefinition.Type => Type;

    /// <summary>
    /// Gets metadata about this field in its source schemas.
    /// Each entry in the collection provides information about this field
    /// that is specific to the source schemas the field was composed of.
    /// </summary>
    public SourceObjectFieldCollection Sources
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Gets the event-stream metadata associated with this composed subscription field.
    /// </summary>
    public EventStreamDirective? EventStreamDirective
    {
        get
        {
            foreach (var source in Sources.Members)
            {
                if (source.EventStreamDirective is { } eventStreamDirective)
                {
                    return eventStreamDirective;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the authorization policy applications for this field.
    /// </summary>
    public ImmutableArray<PolicyApplication> PolicyApplications
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Gets the feature collection associated with this field.
    /// </summary>
    public IFeatureCollection Features
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeOutputFieldCompletionContext context)
    {
        EnsureNotSealed(_completed);

        if (context.Directives is null
            || context.Type is null
            || context.Sources is null
            || context.DeclaringType is null
            || context.Features is null)
        {
            throw InvalidCompletionContext();
        }

        Directives = context.Directives;
        Type = context.Type;
        Sources = context.Sources;
        DeclaringType = context.DeclaringType;
        PolicyApplications = context.PolicyApplications;
        Features = context.Features;
        _completed = true;
    }

    /// <summary>
    /// Gets the string representation of this field definition.
    /// </summary>
    /// <returns>
    /// The string representation of this field definition.
    /// </returns>
    public override string ToString()
        => ToSyntaxNode().ToString(indented: true);

    /// <summary>
    /// Creates a <see cref="FieldDefinitionNode"/> from a
    /// <see cref="FusionOutputFieldDefinition"/>.
    /// </summary>
    public FieldDefinitionNode ToSyntaxNode()
        => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => Format(this);

    [Flags]
    private enum FieldDefinitionFlags : byte
    {
        None = 0,
        Introspection = 1,
        Inaccessible = 2,
        GatewayField = 4
    }
}
