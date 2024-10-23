namespace HotChocolate;

/// <summary>
/// Provides well-known directive names.
/// </summary>
public static class WellKnownDirectives
{
    /// <summary>
    /// The name of the @skip directive.
    /// </summary>
    public const string Skip = "skip";

    /// <summary>
    /// The name of the @include directive.
    /// </summary>
    public const string Include = "include";

    /// <summary>
    /// The name of the @defer directive.
    /// </summary>
    public const string Defer = "defer";

    /// <summary>
    /// The name of the @stream directive.
    /// </summary>
    public const string Stream = "stream";

    /// <summary>
    /// The name of the @oneOf directive.
    /// </summary>
    public const string OneOf = "oneOf";

    /// <summary>
    /// The name of the if directive argument.
    /// </summary>
    public const string IfArgument = "if";

    /// <summary>
    /// The name of the label directive argument.
    /// </summary>
    public const string LabelArgument = "label";

    /// <summary>
    /// The name of the initialCount directive argument.
    /// </summary>
    public const string InitialCount = "initialCount";

    /// <summary>
    /// The name of the @deprecated directive.
    /// </summary>
    public const string Deprecated = "deprecated";

    /// <summary>
    /// The name of the deprecated directive argument.
    /// </summary>
    public const string DeprecationReasonArgument = "reason";

    /// <summary>
    /// The deprecation default reason.
    /// </summary>
    public const string DeprecationDefaultReason = "No longer supported.";

    /// <summary>
    /// The name of the @tag directive.
    /// </summary>
    public const string Tag = "tag";

    /// <summary>
    /// The name of the @tag argument name.
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// The name of the @semanticNonNull directive.
    /// </summary>
    public const string SemanticNonNull = "semanticNonNull";

    /// <summary>
    /// The name of the @semanticNonNull argument levels.
    /// </summary>
    public const string Levels = "levels";
}
