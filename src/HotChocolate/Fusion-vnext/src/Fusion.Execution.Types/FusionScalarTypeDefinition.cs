using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionScalarTypeDefinition : IScalarTypeDefinition
{
    private FusionDirectiveCollection _directives = null!;
    private bool _completed;

    public FusionScalarTypeDefinition(
        string name,
        string? description)
    {
        Name = name;
        Description = description;

        // these properties are initialized
        // in the type complete step.
        Features = null!;
    }

    public TypeKind Kind => TypeKind.Scalar;

    public string Name { get; }

    public string? Description { get; }

    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public FusionDirectiveCollection Directives
    {
        get => _directives;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            _directives = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives;

    public Uri? SpecifiedBy { get; private set; }

    public ScalarSerializationType SerializationType { get; private set; }

    public string? Pattern { get; private set; }

    public ScalarValueKind ValueKind { get; private set; }

    public IFeatureCollection Features
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeScalarTypeCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);
        Directives = context.Directives;
        ValueKind = context.ValueKind;
        SpecifiedBy = context.SpecifiedBy;

        // if the value kind is any, we need to determine the value kind based on the name
        // for the spec scalars.
        if (ValueKind is ScalarValueKind.Any)
        {
            ValueKind = Name switch
            {
                "ID" => ScalarValueKind.String | ScalarValueKind.Integer,
                "String" => ScalarValueKind.String,
                "Int" => ScalarValueKind.Integer,
                "Float" => ScalarValueKind.Float,
                "Boolean" => ScalarValueKind.Boolean,
                _ => ScalarValueKind.Any
            };

            SerializationType = Name switch
            {
                "ID" => ScalarSerializationType.String | ScalarSerializationType.Int,
                "String" => ScalarSerializationType.String,
                "Int" => ScalarSerializationType.Int,
                "Float" => ScalarSerializationType.Float,
                "Boolean" => ScalarSerializationType.Boolean,
                _ => ScalarSerializationType.Undefined
            };
        }

        var serializeAs = Directives.FirstOrDefault(DirectiveNames.SerializeAs.Name);
        if (serializeAs is { Arguments: { Count: 1 } or { Count: 2 } })
        {
            var type = ScalarSerializationType.Undefined;
            string? pattern = null;

            var typeArg = serializeAs.Arguments.FirstOrDefault(
                t => t.Name.Equals(DirectiveNames.SerializeAs.Arguments.Type));
            var patternArg = serializeAs.Arguments.FirstOrDefault(
                t => t.Name.Equals(DirectiveNames.SerializeAs.Arguments.Pattern));

            switch (typeArg?.Value)
            {
                case ListValueNode typeList
                    when typeList.Items.All(t => t.Kind is SyntaxKind.EnumValue):
                    foreach (var item in typeList.Items)
                    {
                        var value = (EnumValueNode)item;
                        if (Enum.TryParse<ScalarSerializationType>(
                            value.Value,
                            ignoreCase: true,
                            out var parsedType))
                        {
                            type |= parsedType;
                        }
                    }
                    break;

                case EnumValueNode singleType
                    when Enum.TryParse<ScalarSerializationType>(
                        singleType.Value,
                        ignoreCase: true,
                        out var parsedType):
                    type = parsedType;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Cannot parse the @serializeAs directive as it is missing the type argument.");
            }

            if (patternArg?.Value is StringValueNode patterValue)
            {
                pattern = patterValue.Value;
            }

            SerializationType = type;
            Pattern = pattern;
        }

        _completed = true;
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.Scalar)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    /// <inheritdoc />
    public bool IsInstanceOfType(IValueNode value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (ValueKind == ScalarValueKind.Any)
        {
            return true;
        }

        return value.Kind switch
        {
            SyntaxKind.NullValue => true,
            SyntaxKind.EnumValue => false,
            SyntaxKind.StringValue => ValueKind.HasFlag(ScalarValueKind.String),
            SyntaxKind.IntValue => ValueKind.HasFlag(ScalarValueKind.Integer),
            SyntaxKind.FloatValue => ValueKind.HasFlag(ScalarValueKind.Float),
            SyntaxKind.BooleanValue => ValueKind.HasFlag(ScalarValueKind.Boolean),
            SyntaxKind.ListValue => ValueKind.HasFlag(ScalarValueKind.List),
            SyntaxKind.ObjectValue => ValueKind.HasFlag(ScalarValueKind.Object),
            _ => false
        };
    }

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is FusionScalarTypeDefinition otherScalar
            && otherScalar.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="ScalarTypeDefinitionNode"/>
    /// from a <see cref="FusionScalarTypeDefinition"/>.
    /// </summary>
    public ScalarTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
