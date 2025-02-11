using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class FusionComplexType : IComplexTypeDefinition
{
    private DirectiveCollection _directives = default!;
    private CompositeInterfaceTypeCollection _implements = default!;
    private ISourceComplexTypeCollection<ISourceComplexType> _sources = default!;
    private bool _completed;
    private IReadOnlyDirectiveCollection _directives1;
    private IReadOnlyInterfaceTypeDefinitionCollection _implements1;
    private IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> _fields;

    protected FusionComplexType(
        string name,
        string? description,
        FusionOutputFieldDefinitionCollection fieldsDefinition)
    {
        Name = name;
        Description = description;
        FieldsDefinition = fieldsDefinition;
    }

    public abstract TypeKind Kind { get; }

    public abstract bool IsEntity { get; }

    public string Name { get; }

    public string? Description { get; }

    public DirectiveCollection Directives
    {
        get => _directives;
        private protected set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _directives = value;
        }
    }

        IReadOnlyDirectiveCollection IDirectivesProvider.Directives
    {
        get => _directives1;
    }


    /// <summary>
    /// Gets the interfaces that are implemented by this type.
    /// </summary>
    public CompositeInterfaceTypeCollection Implements
    {
        get => _implements;
        private protected set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _implements = value;
        }
    }

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> IComplexTypeDefinition.Fields
    {
        get => _fields;
    }

    IReadOnlyInterfaceTypeDefinitionCollection IComplexTypeDefinition.Implements
    {
        get => _implements1;
    }

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public FusionOutputFieldDefinitionCollection FieldsDefinition { get; }

    /// <summary>
    /// Gets the source type definition of this type.
    /// </summary>
    /// <value>
    /// The source type definition of this type.
    /// </value>
    public ISourceComplexTypeCollection<ISourceComplexType> Sources
    {
        get => _sources;
        private protected set
        {
            if (_completed)
            {
                throw new NotSupportedException(
                    "The type definition is sealed and cannot be modified.");
            }

            _sources = value;
        }
    }

    private protected void Complete()
    {
        if (_completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }

        _completed = true;
    }

    public bool Equals(IType? other)
    {
        throw new NotImplementedException();
    }


    public ISyntaxNode ToSyntaxNode()
    {
        throw new NotImplementedException();
    }

    public bool IsAssignableFrom(ITypeDefinition type)
    {
        throw new NotImplementedException();
    }
}
