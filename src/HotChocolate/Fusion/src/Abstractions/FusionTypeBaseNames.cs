namespace HotChocolate.Fusion;

/// <summary>
/// The base type names for the fusion gateway.
/// </summary>
internal static class FusionTypeBaseNames
{
    /// <summary>
    /// The base name of the ArgumentDefinition input.
    /// </summary>
    public const string ArgumentDefinition = "ArgumentDefinition";

    /// <summary>
    /// The base name of the declare directive.
    /// </summary>
    public const string DeclareDirective = "declare";

    /// <summary>
    /// The base name of the fusion directive.
    /// </summary>
    public const string FusionDirective = "fusion";

    /// <summary>
    /// The base name of the is directive which is used during composition.
    /// </summary>
    public const string IsDirective = "is";

    /// <summary>
    /// The base name of the GraphQL name scalar.
    /// </summary>
    public const string Name = "TypeName";

    /// <summary>
    /// The base name of the node directive.
    /// </summary>
    public static string NodeDirective = "node";

    /// <summary>
    /// The base name of the GraphQL operation definition scalar.
    /// </summary>
    public const string OperationDefinition = "OperationDefinition";

    /// <summary>
    /// The base name of the private directive which is used during composition.
    /// </summary>
    public const string PrivateDirective = "private";

    /// <summary>
    /// The base name of the reEncodeId directive.
    /// </summary>
    public static string ReEncodeIdDirective = "reEncodeId";

    /// <summary>
    /// The base name of the remove directive.
    /// </summary>
    public const string RemoveDirective = "remove";
    
    /// <summary>
    /// The base name of the rename directive.
    /// </summary>
    public const string RenameDirective = "rename";

    /// <summary>
    /// The base name of the resolver directive.
    /// </summary>
    public const string ResolverDirective = "resolver";

    /// <summary>
    /// The base name of the ResolverKind input.
    /// </summary>
    public const string ResolverKind = "ResolverKind";

    /// <summary>
    /// The base name for the resolve directive which is used during composition.
    /// </summary>
    public const string ResolveDirective = "resolve";

    /// <summary>
    /// The base name of the require directive.
    /// </summary>
    public const string RequireDirective = "require"; // Note: the value seems to be a mistake as it's the same as RenameDirective's value.

    /// <summary>
    /// The base name of the GraphQL selection scalar.
    /// </summary>
    public const string Selection = "Selection";

    /// <summary>
    /// The base name of the GraphQL selection set scalar.
    /// </summary>
    public const string SelectionSet = "SelectionSet";

    /// <summary>
    /// The base name of the schema coordinate type.
    /// </summary>
    public const string SchemaCoordinate = "SchemaCoordinate";

    /// <summary>
    /// The base name of the source directive.
    /// </summary>
    public const string SourceDirective = "source";

    /// <summary>
    /// The base name of the transport directive.
    /// </summary>
    public const string TransportDirective = "transport";

    /// <summary>
    /// The base name of the GraphQL type scalar.
    /// </summary>
    public const string Type = "Type";

    /// <summary>
    /// The base name of the URI scalar.
    /// </summary>
    public const string Uri = "Uri";

    /// <summary>
    /// The base name of the variable directive.
    /// </summary>
    public const string VariableDirective = "variable";
}


