using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
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

    /// <summary>
    /// Initializes a new instance of <see cref="FusionOutputFieldDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the field.</param>
    /// <param name="description">The description of the field.</param>
    /// <param name="isDeprecated">A value indicating whether the field is deprecated.</param>
    /// <param name="deprecationReason">The deprecation reason if the field is deprecated.</param>
    /// <param name="isInaccessible">A value indicating whether the field is marked as inaccessible.</param>
    /// <param name="arguments">The collection of arguments for this field.</param>
    public FusionOutputFieldDefinition(
        string name,
        string? description,
        bool isDeprecated,
        string? deprecationReason,
        bool isInaccessible,
        FusionInputFieldDefinitionCollection arguments)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(arguments);

        Name = name;
        Description = description;
        IsDeprecated = isDeprecated;
        IsIntrospectionField = name.StartsWith("__");
        DeprecationReason = deprecationReason;
        IsInaccessible = isInaccessible;
        Arguments = arguments;

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
    /// Gets a value indicating whether this field is deprecated.
    /// </summary>
    public bool IsDeprecated { get; }

    /// <summary>
    /// Gets a value indicating whether this field is an introspection field.
    /// </summary>
    public bool IsIntrospectionField { get; }

    /// <summary>
    /// Gets the deprecation reason if the field is deprecated.
    /// </summary>
    public string? DeprecationReason { get; }

    /// <summary>
    /// Gets a value indicating whether this field is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

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
}
