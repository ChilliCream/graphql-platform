using HotChocolate.Language;

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
}