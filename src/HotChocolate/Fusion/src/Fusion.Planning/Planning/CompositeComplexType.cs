using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents the base class for a GraphQL object type definition or an interface type definition.
/// </summary>
public abstract class CompositeComplexType : ICompositeType
{
    private DirectiveCollection _directives = default!;
    private CompositeInterfaceTypeCollection _implements = default!;
    private bool _completed;

    protected CompositeComplexType(string name, string? description, CompositeObjectFieldCollection fields)
    {
        Name = name;
        Description = description;
        Fields = fields;
    }

    public abstract TypeKind Kind { get; }

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

    /// <summary>
    /// Gets the fields of this type.
    /// </summary>
    /// <value>
    /// The fields of this type.
    /// </value>
    public CompositeObjectFieldCollection Fields { get; }

    private protected void Complete() => _completed = true;
}
