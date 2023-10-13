using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of
/// `directive @remove(coordinate: _SchemaCoordinate) ON SCHEMA`.
/// </summary>
/// <param name="coordinate">
/// A reference to the type system member that shall be removed.
/// </param>
internal sealed class RemoveDirective(SchemaCoordinate coordinate)
{
    /// <summary>
    /// Gets the coordinate that refers to the type system member that shall be removed.
    /// </summary>
    public SchemaCoordinate Coordinate { get; } = coordinate;
    
    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="RemoveDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new Directive(
            context.RemoveDirective, 
            new Argument(CoordinateArg, Coordinate.ToString()));
    }

    /// <summary>
    /// Tries to parse a <see cref="RemoveDirective"/> from a <see cref="Directive"/>.
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
        [NotNullWhen(true)] out RemoveDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.RemoveDirective.Name))
        {
            directive = null;
            return false;
        }

        var coordinate = directiveNode.Arguments
            .GetValueOrDefault(CoordinateArg)
            ?.Value;

        if (coordinate is StringValueNode coordinateValue)
        {
            directive = new RemoveDirective(SchemaCoordinate.Parse(coordinateValue.Value));
            return true;
        }
        
        directive = null;
        return false;
    }
    
    /// <summary>
    /// Gets all @remove directives from the specified member.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// Returns all @remove directives.
    /// </returns>
    public static IEnumerable<RemoveDirective> GetAllFrom(
        IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.RemoveDirective.Name])
        {
            if (TryParse(directive, context, out var removeDirective))
            {
                yield return removeDirective;
            }
        }
    }
    
    /// <summary>
    /// Checks if the specified member has a @remove directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @remove directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return member.Directives.ContainsName(context.RemoveDirective.Name);
    }
}
