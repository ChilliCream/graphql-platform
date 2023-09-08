namespace HotChocolate.Fusion.Clients;

/// <summary>
/// Specifies the transport features that are needed for a GraphQL request.
/// </summary>
[Flags]
public enum TransportFeatures
{
    /// <summary>
    /// Standard GraphQL over HTTP POST request.
    /// </summary>
    Standard = 0,
    
    /// <summary>
    /// GraphQL multipart request.
    /// </summary>
    FileUpload = 1,
    
    /// <summary>
    /// All Features.
    /// </summary>
    All = Standard | FileUpload
}