namespace HotChocolate.Fusion.Types;

/// <summary>
/// The fusion schema options.
/// </summary>
public interface IFusionSchemaOptions
{
    /// <summary>
    /// Applies the @serializeAs directive to scalar types that specify a serialization format.
    /// </summary>
    bool ApplySerializeAsToScalars { get; }

    /// <summary>
    /// Gets whether <c>@defer</c> is enabled.
    /// When <c>false</c>, the <c>@defer</c> directive is not exposed in the schema
    /// and deferred execution is disabled.
    /// </summary>
    bool EnableDefer { get; }

    /// <summary>
    /// Enables the <c>__search</c> and <c>__definitions</c> introspection fields
    /// for semantic schema discovery.
    /// </summary>
    bool EnableSemanticIntrospection { get; }
}
