using System.Diagnostics.CodeAnalysis;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the runtime value of 
/// `directive @source(
///     subgraph: Name!,
///     name: Name
///  ) repeatable on OBJECT | FIELD_DEFINITION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION | SCALAR`.
/// </summary>
internal sealed class SourceDirective
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceDirective"/> class.
    /// </summary>
    /// <param name="subgraph">The name of the subgraph.</param>
    /// <param name="name">The name of the source.</param>
    public SourceDirective(string subgraph, string? name = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(subgraph);
        
        Subgraph = subgraph;
        Name = name;
    }

    /// <summary>
    /// Gets the name of the subgraph.
    /// </summary>
    public string Subgraph { get; }

    /// <summary>
    /// Gets the name of the source.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="SourceDirective"/>.
    /// </summary>
    /// <param name="context">The fusion type context that provides the directive names.</param>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var args = Name is null ? new Argument[1] : new Argument[2];

        args[0] = new Argument(SubgraphArg, Subgraph);

        if (Name is not null)
        {
            args[1] = new Argument(NameArg, Name);
        }

        return new Directive(context.SourceDirective, args);
    }

    /// <summary>
    /// Tries to parse a <see cref="SourceDirective"/> from a <see cref="Directive"/>.
    /// </summary>
    public static bool TryParse(
        Directive directiveNode,
        IFusionTypeContext context,
        [NotNullWhen(true)] out SourceDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.SourceDirective.Name))
        {
            directive = null;
            return false;
        }

        var subgraph = directiveNode.Arguments.GetValueOrDefault(SubgraphArg)?.ExpectStringLiteral();
        var name = directiveNode.Arguments.GetValueOrDefault(NameArg)?.ExpectStringLiteral();

        if (subgraph is null)
        {
            directive = null;
            return false;
        }

        directive = new SourceDirective(subgraph.Value, name?.Value);
        return true;
    }
    
    /// <summary>
    /// Gets all @source directives from the specified member.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// Returns all @source directives.
    /// </returns>
    public static IEnumerable<SourceDirective> GetAllFrom(
        IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.SourceDirective.Name])
        {
            if (TryParse(directive, context, out var declareDirective))
            {
                yield return declareDirective;
            }
        }
    }
    
    /// <summary>
    /// Checks if the specified member has a @source directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @source directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return member.Directives.ContainsName(context.SourceDirective.Name);
    }

    /// <summary>
    /// Creates the source directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        var nameType = new MissingType(FusionTypeBaseNames.Name);
        
        var directiveType = new DirectiveType(FusionTypeBaseNames.SourceDirective)
        {
            Locations = DirectiveLocation.Object | 
                DirectiveLocation.FieldDefinition | 
                DirectiveLocation.Enum | 
                DirectiveLocation.EnumValue | 
                DirectiveLocation.InputObject | 
                DirectiveLocation.InputFieldDefinition | 
                DirectiveLocation.Scalar,
            IsRepeatable = true,
            Arguments =
            {
                new InputField(SubgraphArg, new NonNullType(nameType)),
                new InputField(NameArg, nameType)
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };

        return directiveType;
    }
}
