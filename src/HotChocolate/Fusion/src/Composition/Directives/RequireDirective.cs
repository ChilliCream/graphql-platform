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
/// Represents the @require directive. 
/// </summary>
/// <param name="field">
/// The field syntax that refers to the required field.
/// </param>
internal sealed class RequireDirective(FieldNode field)
{
    /// <summary>
    /// Initializes a new instance of <see cref="RequireDirective"/>.
    /// </summary>
    /// <param name="fieldSyntax">
    /// The field selection syntax which specifies the requirement.
    /// </param>
    public RequireDirective(string fieldSyntax)
        : this(Utf8GraphQLParser.Syntax.ParseField(fieldSyntax))
    {
    }
    
    /// <summary>
    /// Gets the field selection syntax which specifies the requirement.
    /// </summary>
    public FieldNode Field { get; } = field;
    
    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="RequireDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// The created directive.
    /// </returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return new Directive(
            context.RequireDirective, 
            new Argument(FieldArg, new StringValueNode(Field.ToString(false))));
    }
    
    /// <summary>
    /// Tries to parse a <see cref="RequireDirective"/> from a <see cref="Directive"/>.
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
        [NotNullWhen(true)] out RequireDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);

        if (!directiveNode.Name.EqualsOrdinal(context.RequireDirective.Name))
        {
            directive = null;
            return false;
        }

        var field = directiveNode.Arguments
            .GetValueOrDefault(FieldArg)?
            .ExpectFieldSelection();
        
        if(field is null)
        {
            directive = null;
            return false;
        }

        directive = new RequireDirective(field);
        return true;
    }
    
    /// <summary>
    /// Gets the @require directive from the specified member.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// The @require directive.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The member does not have a @require directive.
    /// </exception>
    public static RequireDirective GetFrom(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        var directive = member.Directives[context.RequireDirective.Name].First();

        if (TryParse(directive, context, out var requireDirective))
        {
            return requireDirective;
        }

        throw new InvalidOperationException(RequireDirective_GetFrom_DirectiveNotValid);
    }
    
    /// <summary>
    /// Checks if the specified member has a @require directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @require directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(nameof(member));
        ArgumentNullException.ThrowIfNull(nameof(context));
        
        return member.Directives.ContainsName(context.RequireDirective.Name);
    }
    
    /// <summary>
    /// Creates the rename directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        // directive @require(
        //   field: Selection
        // ) on ARGUMENT_DEFINITION
        
        var selectionType = new MissingType(FusionTypeBaseNames.Selection);
        
        var directiveType = new DirectiveType(FusionTypeBaseNames.RequireDirective)
        {
            Locations = DirectiveLocation.ArgumentDefinition,
            IsRepeatable = false,
            Arguments =
            {
                new InputField(FieldArg, new NonNullType(selectionType))
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };
        
        return directiveType;
    }
}