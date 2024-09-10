using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class SdlToTypeSystemHelper
{
    public static void AddDirectives<TOwner>(
        IDescriptorContext context,
        TOwner owner,
        HotChocolate.Language.IHasDirectives ownerSyntax,
        Stack<IDefinition> path)
        where TOwner : IHasDirectiveDefinition, IDefinition
    {
        foreach (var directive in ownerSyntax.Directives)
        {
            if (context.TryGetSchemaDirective(directive, out var schemaDirective))
            {
                schemaDirective.ApplyConfiguration(context, directive, owner, path);
                continue;
            }

            if (directive.IsDeprecationReason() || directive.IsBindingDirective())
            {
                continue;
            }

            owner.Directives.Add(new(directive));
        }
    }

    public static string? DeprecationReason(
        this Language.IHasDirectives syntaxNode)
    {
        var directive = syntaxNode.Directives.FirstOrDefault(
            t => t.Name.Value == WellKnownDirectives.Deprecated);

        if (directive is null)
        {
            return null;
        }

        if (directive.Arguments.Count != 0
            && directive.Arguments[0].Name.Value == WellKnownDirectives.DeprecationReasonArgument
            && directive.Arguments[0].Value is StringValueNode s
            && !string.IsNullOrEmpty(s.Value))
        {
            return s.Value;
        }

        return WellKnownDirectives.DeprecationDefaultReason;
    }

    public static bool IsDeprecationReason(this DirectiveNode directiveNode)
        => string.Equals(directiveNode.Name.Value,
            WellKnownDirectives.Deprecated,
            StringComparison.Ordinal);
}
