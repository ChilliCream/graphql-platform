using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Composition.Properties.CompositionResources;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a directive that defines semantic equivalence between two
/// fields or a field and an argument.
/// </summary>
internal sealed class IsDirective : IEquatable<IsDirective>
{
    /// <summary>
    /// Creates a new instance of <see cref="IsDirective"/> that
    /// uses a schema coordinate to refer to a field or argument.
    /// </summary>
    /// <param name="coordinate">
    /// A schema coordinate that refers to another field or argument.
    /// </param>
    public IsDirective(SchemaCoordinate coordinate)
    {
        Coordinate = coordinate;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IsDirective"/> that a field syntax to refer to field.
    /// </summary>
    /// <param name="field">
    /// The field selection syntax that refers to another field.
    /// </param>
    public IsDirective(FieldNode field)
    {
        Field = field;
    }

    /// <summary>
    /// Returns <c>true</c> if this directive refers to a schema coordinate.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Coordinate))]
    [MemberNotNullWhen(false, nameof(Field))]
    public bool IsCoordinate => Field is null;

    /// <summary>
    /// A schema coordinate that refers to another field or argument.
    /// </summary>
    public SchemaCoordinate? Coordinate { get; }

    /// <summary>
    /// If used on an argument this field selection syntax refers to a
    /// field of the return type of the declaring field.
    /// </summary>
    public FieldNode? Field { get; }

    public bool Equals(IsDirective? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!Nullable.Equals(Coordinate, other.Coordinate))
        {
            return false;
        }

        if (Field is null)
        {
            return other.Field is null;
        }

        if (other.Field is null)
        {
            return false;
        }

        return Field.Equals(other.Field, SyntaxComparison.Syntax);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || obj is IsDirective other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(
            Coordinate,
            Field is null
                ? 0
                : SyntaxComparer.BySyntax.GetHashCode(Field));

    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="IsDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var args = Coordinate is not null
            ? new Argument[1]
            : new Argument[2];

        if (Coordinate is not null)
        {
            args[0] = new Argument(CoordinateArg, new StringValueNode(Coordinate.ToString()!));
        }
        else
        {
            args[0] = new Argument(FieldArg, new StringValueNode(Field!.ToString(false)));
        }

        return new Directive(context.IsDirective, args);
    }

    /// <summary>
    /// Tries to parse a <see cref="IsDirective"/> from a <see cref="Directive"/>.
    /// </summary>
    /// <param name="directiveNode">
    /// The directive node that shall be parsed.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <param name="directive">
    /// The parsed directive.
    /// </param>
    /// <returns>
    /// <c>true</c> if the directive could be parsed; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryParse(
        Directive directiveNode,
        IFusionTypeContext context,
        [NotNullWhen(true)] out IsDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.IsDirective.Name))
        {
            directive = null;
            return false;
        }

        var coordinate = directiveNode.Arguments.GetValueOrDefault(CoordinateArg);

        if (coordinate is StringValueNode coordinateValue)
        {
            directive = new IsDirective(SchemaCoordinate.Parse(coordinateValue.Value));
            return true;
        }

        var field = directiveNode.Arguments.GetValueOrDefault(FieldArg);

        if (field is StringValueNode fieldValue)
        {
            directive = new IsDirective(Utf8GraphQLParser.Syntax.ParseField(fieldValue.Value));
            return true;
        }

        directive = null;
        return false;
    }

    public static IsDirective GetFrom(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));

        var directive = member.Directives[context.IsDirective.Name].First();

        if (TryParse(directive, context, out var result))
        {
            return result;
        }

        throw new InvalidOperationException(IsDirective_GetFrom_DirectiveNotValid);
    }

    public static IsDirective? TryGetFrom(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));

        var directive = member.Directives[context.IsDirective.Name].First();

        if (TryParse(directive, context, out var result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    /// Checks if the specified member has a @is directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @is directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));

        return member.Directives.ContainsName(context.IsDirective.Name);
    }

    /// <summary>
    /// Creates the is directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        /*
         * directive @is(
         *   field: Selection
         *   coordinate: SchemaCoordinate
         * ) on FIELD_DEFINITION | ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION
         */

        var selectionType = new MissingType(FusionTypeBaseNames.Selection);
        var schemaCoordinate = new MissingType(FusionTypeBaseNames.SchemaCoordinate);

        var directiveType = new DirectiveType(FusionTypeBaseNames.IsDirective)
        {
            Locations = DirectiveLocation.FieldDefinition |
                DirectiveLocation.ArgumentDefinition |
                DirectiveLocation.InputFieldDefinition,
            IsRepeatable = false,
            Arguments =
            {
                new InputField(FieldArg, new NonNullType(selectionType)),
                new InputField(CoordinateArg, new NonNullType(schemaCoordinate))
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };

        return directiveType;
    }
}