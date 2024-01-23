namespace HotChocolate.AspNetCore;

/// <summary>
/// Representation of well-known media kinds. We use this to avoid constant string comparison.
/// </summary>
public enum AcceptMediaTypeKind
{
    /// <summary>
    /// Not a well-known meda type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// *.*
    /// </summary>
    All,

    /// <summary>
    /// application/*
    /// </summary>
    AllApplication,

    /// <summary>
    /// multipart/*
    /// </summary>
    AllMultiPart,

    /// <summary>
    /// application/graphql-response+json
    /// </summary>
    ApplicationGraphQL,

    /// <summary>
    /// application/json
    /// </summary>
    ApplicationJson,

    /// <summary>
    /// multipart/mixed
    /// </summary>
    MultiPartMixed,

    /// <summary>
    /// text/event-stream
    /// </summary>
    EventStream,
}
