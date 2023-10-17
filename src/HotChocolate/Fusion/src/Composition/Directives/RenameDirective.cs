using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of
/// `directive @rename(coordinate: SchemaCoordinate, newName: Name!) repeatable ON SCHEMA`.
/// </summary>
internal sealed class RenameDirective
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="coordinate">
    /// A reference to the type system member that shall be renamed.
    /// </param>
    /// <param name="newName">
    /// The new name that shall be applied to the type system member.
    /// </param>
    public RenameDirective(SchemaCoordinate coordinate, string newName)
    {
        ArgumentException.ThrowIfNullOrEmpty(newName);
        
        Coordinate = coordinate;
        NewName = newName;
    }

    /// <summary>
    /// Gets the coordinate that refers to the type system member that shall be renamed.
    /// </summary>
    public SchemaCoordinate Coordinate { get; }
    
    /// <summary>
    /// Gets the new name that shall be applied to that type system member.
    /// </summary>
    public string NewName { get; }
    
    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="RenameDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new Directive(
            context.RenameDirective, 
            new Argument(CoordinateArg, Coordinate.ToString()),
            new Argument(NewNameArg, NewName));
    }

    /// <summary>
    /// Tries to parse a <see cref="RenameDirective"/> from a <see cref="Directive"/>.
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
        [NotNullWhen(true)] out RenameDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.RenameDirective.Name))
        {
            directive = null;
            return false;
        }

        var coordinate = directiveNode.Arguments
            .GetValueOrDefault(CoordinateArg)?
            .ExpectStringLiteral();

        if (coordinate is null)
        {
            directive = null;
            return false;
        }

        var newName = directiveNode.Arguments
            .GetValueOrDefault(NewNameArg)?
            .ExpectStringLiteral();

        if (newName is null)
        {
            directive = null;
            return false;
        }
        
        directive = new RenameDirective(
            SchemaCoordinate.Parse(coordinate.Value),
            newName.Value);
        return false;
    }
    
    /// <summary>
    /// Gets all @rename directives from the specified member.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// Returns all @rename directives.
    /// </returns>
    public static IEnumerable<RenameDirective> GetAllFrom(
        IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.RenameDirective.Name])
        {
            if (TryParse(directive, context, out var renameDirective))
            {
                yield return renameDirective;
            }
        }
    }
    
    /// <summary>
    /// Checks if the specified member has a @rename directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @rename directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return member.Directives.ContainsName(context.RenameDirective.Name);
    }
    
    /// <summary>
    /// Creates the rename directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        /*
         * directive @rename(
         *   coordinate: SchemaCoordinate!
         *   newName: Name!
         * ) repeatable on SCHEMA
         */
        
        var schemaCoordinateType = new MissingType(FusionTypeBaseNames.SchemaCoordinate);
        var nameType = new MissingType(FusionTypeBaseNames.Name);
        
        var directiveType = new DirectiveType(FusionTypeBaseNames.RenameDirective)
        {
            Locations = DirectiveLocation.Schema,
            IsRepeatable = true,
            Arguments =
            {
                new InputField(CoordinateArg, new NonNullType(schemaCoordinateType)),
                new InputField(NewNameArg, new NonNullType(nameType))
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };
        
        return directiveType;
    }
}
