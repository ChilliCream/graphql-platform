using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Skimmed;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.FusionDirectiveArgumentNames;
using DirectiveLocation = HotChocolate.Skimmed.DirectiveLocation;
using IHasDirectives = HotChocolate.Skimmed.IHasDirectives;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents a variable declaration that can be used to declare state for the @resolve directive.
/// </summary>
internal sealed class DeclareDirective
{
    public DeclareDirective(string name, string selectSyntax, string? from = null)
    {
        name.EnsureGraphQLName();
        ArgumentException.ThrowIfNullOrEmpty(selectSyntax);
        from?.EnsureGraphQLName();

        Name = name;
        Select = Utf8GraphQLParser.Syntax.ParseField(selectSyntax);
        From = from;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DeclareDirective"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the variable that shall be declared.
    /// </param>
    /// <param name="select">
    /// The field selection syntax that refers to a field
    /// relative to the current type and specifies it as state.
    /// </param>
    /// <param name="from">
    /// The subgraph the declaration refers to.
    /// If set to <c>null</c> it will match state from all applicable subgraphs.
    /// </param>
    public DeclareDirective(string name, FieldNode select, string? from = null)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(select);
        from?.EnsureGraphQLName();
        
        Name = name;
        Select = select;
        From = from;
    }

    /// <summary>
    /// Gets the name of the variable that shall be declared.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the field selection syntax that refers to a field relative
    /// to the current type and specifies it as state.
    /// </summary>
    public FieldNode Select { get; }

    /// <summary>
    /// Gets the subgraph the declaration refers to.
    /// If set to <c>null</c> it will match state from all applicable subgraphs.
    /// </summary>
    public string? From { get; }

    /// <summary>
    /// Creates a <see cref="Directive"/> from this <see cref="DeclareDirective"/>.
    /// </summary>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns></returns>
    public Directive ToDirective(IFusionTypeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        var args = From is null ? new Argument[2] : new Argument[3];
        
        args[0] = new Argument(NameArg, new StringValueNode(Name));
        args[1] = new Argument(SelectArg, new StringValueNode(Select.ToString(false)));

        if (From is not null)
        {
            args[2] = new Argument(FromArg, new StringValueNode(From));
        }

        return new Directive(context.DeclareDirective, args);
    }

    /// <summary>
    /// Tries to parse a <see cref="DeclareDirective"/> from a <see cref="Directive"/>.
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
        [NotNullWhen(true)] out DeclareDirective? directive)
    {
        ArgumentNullException.ThrowIfNull(directiveNode);
        ArgumentNullException.ThrowIfNull(context);
        
        if (!directiveNode.Name.EqualsOrdinal(context.DeclareDirective.Name))
        {
            directive = null;
            return false;
        }

        var name = directiveNode.Arguments
            .GetValueOrDefault(NameArg)?
            .ExpectStringLiteral()
            .Value;

        if (name is null)
        {
            directive = null;
            return false;
        }

        var select = directiveNode.Arguments
            .GetValueOrDefault(SelectArg)?
            .ExpectFieldSelection();

        if (select is null)
        {
            directive = null;
            return false;
        }

        var from = directiveNode.Arguments
            .GetValueOrDefault(FromArg)?
            .ExpectStringLiteral()
            .Value;

        directive = new DeclareDirective(name, select, from);
        return true;
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
    public static IEnumerable<DeclareDirective> GetAllFrom(
        IHasDirectives member,
        IFusionTypeContext context)
    {
        foreach (var directive in member.Directives[context.DeclareDirective.Name])
        {
            if (TryParse(directive, context, out var declareDirective))
            {
                yield return declareDirective;
            }
        }
    }
    
    /// <summary>
    /// Checks if the specified member has a @declare directive.
    /// </summary>
    /// <param name="member">
    /// The member that shall be checked.
    /// </param>
    /// <param name="context">
    /// The fusion type context that provides the directive names.
    /// </param>
    /// <returns>
    /// <c>true</c> if the member has a @declare directive; otherwise, <c>false</c>.
    /// </returns>
    public static bool ExistsIn(IHasDirectives member, IFusionTypeContext context)
        => member.Directives.ContainsName(context.DeclareDirective.Name);
    
    /// <summary>
    /// Creates the declare directive type.
    /// </summary>
    public static DirectiveType CreateType()
    {
        /*
         * directive @declare(
         *   variable: Name!
         *   select: Selection!
         *   from: Name
         * ) repeatable on FIELD_DEFINITION
         */

        var nameType = new MissingType(FusionTypeBaseNames.Name);
        var selectionType = new MissingType(FusionTypeBaseNames.Selection);
        
        var directiveType = new DirectiveType(FusionTypeBaseNames.DeclareDirective)
        {
            Locations = DirectiveLocation.FieldDefinition,
            IsRepeatable = true,
            Arguments =
            {
                new InputField(VariableArg, new NonNullType(nameType)),
                new InputField(TypeArg, new NonNullType(selectionType)),
                new InputField(FromArg, nameType)
            },
            ContextData =
            {
                [WellKnownContextData.IsFusionType] = true
            }
        };
        
        return directiveType;
    }
}