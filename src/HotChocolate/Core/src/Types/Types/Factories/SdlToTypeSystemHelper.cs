using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Types.Factories;

internal static class SdlToTypeSystemHelper
{
    public static void AddDirectives<TOwner>(
        IDescriptorContext context,
        TOwner owner,
        HotChocolate.Language.IHasDirectives ownerSyntax,
        Stack<ITypeSystemConfiguration> path)
        where TOwner : IDirectiveConfigurationProvider, ITypeSystemConfiguration
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
        this IHasDirectives syntaxNode)
    {
        var directive = syntaxNode.Directives.FirstOrDefault(
            t => t.Name.Value == DirectiveNames.Deprecated.Name);

        if (directive is null)
        {
            return null;
        }

        if (directive.Arguments.Count != 0
            && directive.Arguments[0].Name.Value == DirectiveNames.Deprecated.Arguments.Reason
            && directive.Arguments[0].Value is StringValueNode s
            && !string.IsNullOrEmpty(s.Value))
        {
            return s.Value;
        }

        return DirectiveNames.Deprecated.Arguments.DefaultReason;
    }

    public static bool IsDeprecationReason(this DirectiveNode directiveNode)
        => string.Equals(directiveNode.Name.Value,
            DirectiveNames.Deprecated.Name,
            StringComparison.Ordinal);
}
