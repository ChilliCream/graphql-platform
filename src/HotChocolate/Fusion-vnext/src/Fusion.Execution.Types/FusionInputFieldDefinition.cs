using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionInputFieldDefinition : IInputValueDefinition
{
    private bool _completed;

    public FusionInputFieldDefinition(
        string name,
        string? description,
        IValueNode? defaultValue,
        bool isDeprecated,
        string? deprecationReason)
    {
        Name = name;
        Description = description;
        DefaultValue = defaultValue;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;

        // these properties are initialized
        // in the type complete step.
        DeclaringMember = null!;
        Directives = null!;
        Type = null!;
        Features = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public ITypeSystemMember DeclaringMember
    {
        get;
        set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    public SchemaCoordinate Coordinate
    {
        get
        {
            switch (DeclaringMember)
            {
                case IInputObjectTypeDefinition typeDef:
                    return new SchemaCoordinate(typeDef.Name, Name, ofDirective: false);

                case IDirectiveDefinition directiveDef:
                    return new SchemaCoordinate(directiveDef.Name, Name, ofDirective: true);

                case IOutputFieldDefinition fieldDef:
                    return new SchemaCoordinate(
                        fieldDef.DeclaringType.Name,
                        fieldDef.Name,
                        Name,
                        ofDirective: false);

                default:
                    throw new InvalidOperationException("The declaring type is not set.");
            }
        }
    }

    public IValueNode? DefaultValue { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }

    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    public IInputType Type
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    public FieldFlags Flags => FieldFlags.None;

    IType IFieldDefinition.Type => Type;

    public IFeatureCollection Features
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeInputFieldCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);
        DeclaringMember = context.DeclaringMember;
        Directives = context.Directives;
        Type = context.Type;
        Features = context.Features;
        _completed = true;
    }

    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(indented: true);

    public InputValueDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
