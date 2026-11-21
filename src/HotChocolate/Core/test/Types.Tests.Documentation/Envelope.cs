namespace HotChocolate.Types.Descriptors;

/// <summary>An envelope that carries a payload.</summary>
public class Envelope<TPayload>
{
    /// <summary>The carried payload.</summary>
    public TPayload Payload => default!;

    /// <summary>Gets an attachment.</summary>
    /// <param name="position">The attachment position.</param>
    /// <returns>The attachment.</returns>
    public TPayload GetAttachment(int position) => default!;

    /// <summary>Seals the envelope.</summary>
    /// <param name="attachments">The attachments to seal.</param>
    public bool Seal(IReadOnlyList<TPayload> attachments) => true;

    /// <summary>Unwraps the attachments.</summary>
    /// <param name="attachments">The attachments to unwrap.</param>
    /// <param name="defaultValue">The value used when nothing can be unwrapped.</param>
    public TResult Unwrap<TResult>(TPayload[] attachments, TResult defaultValue) => defaultValue;
}

/// <summary>A query that returns an envelope.</summary>
public class QueryWithEnvelope
{
    /// <summary>Gets the envelope.</summary>
    public Envelope<string> Envelope => new();
}
