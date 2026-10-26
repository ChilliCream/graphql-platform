using HotChocolate.Types;

namespace HotChocolate.Fusion.Extensions;

internal static class DirectiveDefinitionExtensions
{
    /// <summary>
    /// The executable locations at which the gateway passes a directive on to source schemas
    /// together with the field or variable definition that carries it.
    /// </summary>
    public const DirectiveLocation ForwardedExecutableLocations =
        DirectiveLocation.Field | DirectiveLocation.VariableDefinition;

    extension(IDirectiveDefinition directiveDefinition)
    {
        /// <summary>
        /// Gets a value indicating whether the directive is a client directive that composition
        /// can carry into the composite schema: it has a forwarded executable location and is
        /// neither a spec directive nor <c>@defer</c> or <c>@stream</c>.
        /// </summary>
        public bool IsComposableExecutableDirective
            => (directiveDefinition.Locations & ForwardedExecutableLocations) != 0
                && !DirectiveNames.IsSpecDirective(directiveDefinition.Name)
                && directiveDefinition.Name is not (DirectiveNames.Defer.Name or DirectiveNames.Stream.Name);
    }
}
