using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using IHasDirectives = HotChocolate.Language.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// The @resolve directive allows to specify a custom resolver for a field by specifying GraphQL query syntax.
/// </summary>
internal sealed class ResolveDirective
{
    /// <summary>
    /// Creates a new instance of <see cref="ResolveDirective"/>.
    /// </summary>
    /// <param name="select"></param>
    /// <param name="from"></param>
    public ResolveDirective(FieldNode select, string? from = null)
    {
        Select = select;
        From = from;
    }

    /// <summary>
    /// Gets the field selection syntax that refers to a root query field.
    /// However, if @resolve is used on a mutation or subscription root field
    /// this select syntax refers to a mutation or subscription root field.
    /// </summary>
    public FieldNode Select { get; }
    
    /// <summary>
    /// Specifies the subgraph the field syntax refers to.
    /// If set to null it shall refer to all subgraphs and match
    /// which subgraphs are able to provide the resolver.
    /// </summary>
    public string? From { get; }
    
    /// <summary>
    /// Creates a <see cref="Skimmed.Directive"/> from this <see cref="RenameDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var args = From is null ? new Argument[1] : new Argument[2];
        args[0] = new Argument(SelectArg, new StringValueNode(Select.ToString(false)));

        if (From is not null)
        {
            args[1] = new Argument(FromArg, SubgraphArg);
        }

        return new Directive(context.RenameDirective, args);
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
        [NotNullWhen(true)] out ResolveDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.ResolveDirective.Name))
        {
            directive = null;
            return false;
        }

        var select = directiveNode.Arguments
            .GetValueOrDefault(SelectArg)?
            .ExpectStringLiteral();

        if (select is null)
        {
            directive = null;
            return false;
        }

        var from = directiveNode.Arguments
            .GetValueOrDefault(FromArg)?
            .ExpectStringOrNullLiteral();
        
        directive = new ResolveDirective(
            Utf8GraphQLParser.Syntax.ParseField(select.Value),
            from?.Value);
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
    public static IEnumerable<ResolveDirective> GetAllFrom(
        Skimmed.IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.ResolveDirective.Name])
        {
            if (TryParse(directive, context, out var resolveDirective))
            {
                yield return resolveDirective;
            }
        }
    }
    
    /// <summary>
    /// Checks if the specified member has a @resolve directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @resolve directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(Skimmed.IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return member.Directives.ContainsName(context.ResolveDirective.Name);
    }
}