using System.Text.Json;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Reads the session file that the Nitro CLI writes when a user signs in with
/// <c>nitro login</c> and persists refreshed tokens back to it.
/// </summary>
internal sealed class NitroSessionReader
{
    private readonly string _sessionFilePath;
    private readonly TimeSpan _rereadDelay;

    /// <summary>
    /// Initializes a new instance of <see cref="NitroSessionReader"/>.
    /// </summary>
    /// <param name="sessionFilePath">
    /// The full path of the Nitro CLI session file.
    /// </param>
    /// <param name="rereadDelay">
    /// The delay before the single re-read that resolves a read which raced with
    /// <c>nitro login</c> rewriting the file.
    /// </param>
    public NitroSessionReader(string sessionFilePath, TimeSpan rereadDelay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionFilePath);

        if (rereadDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rereadDelay),
                rereadDelay,
                "The re-read delay must not be negative.");
        }

        _sessionFilePath = sessionFilePath;
        _rereadDelay = rereadDelay;
    }

    /// <summary>
    /// Gets the full path of the Nitro CLI session file.
    /// </summary>
    public string SessionFilePath => _sessionFilePath;

    /// <summary>
    /// Reads the session file.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// The outcome of the read. A session that carries no access token or no expiry is reported
    /// as <see cref="NitroSessionStatus.Unusable"/>, never as a session.
    /// </returns>
    public async Task<NitroSessionReadResult> ReadAsync(CancellationToken cancellationToken)
    {
        var (result, transient) = await ReadOnceAsync(cancellationToken);

        if (transient)
        {
            // `nitro login` replaces the session file in place, so a read can observe a
            // partially written document or a locked file. A single re-read settles that race.
            await Task.Delay(_rereadDelay, cancellationToken);

            (result, _) = await ReadOnceAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Atomically writes a refreshed session to the Nitro CLI session file.
    /// </summary>
    /// <param name="session">
    /// The refreshed session.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    public async Task WriteAsync(NitroSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var directory = IOPath.GetDirectoryName(_sessionFilePath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException(
                $"The Nitro session path '{_sessionFilePath}' has no parent directory.");
        }

        Directory.CreateDirectory(directory);
        var temporaryFilePath = IOPath.Combine(
            directory,
            $".{IOPath.GetFileName(_sessionFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryFilePath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous
                }))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    session,
                    NitroJsonContext.Default.NitroSession,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryFilePath, _sessionFilePath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryFilePath);
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                // A temporary file that cannot be removed is left for the operating system.
            }
        }
    }

    private async Task<(NitroSessionReadResult Result, bool Transient)> ReadOnceAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_sessionFilePath))
        {
            return (
                new NitroSessionReadResult(
                    NitroSessionStatus.Missing,
                    Session: null,
                    $"No Nitro session file was found at '{_sessionFilePath}'."),
                false);
        }

        NitroSession? session;

        try
        {
            await using var stream = new FileStream(
                _sessionFilePath,
                new FileStreamOptions
                {
                    Mode = FileMode.Open,
                    Access = FileAccess.Read,
                    Share = FileShare.ReadWrite | FileShare.Delete,
                    Options = FileOptions.Asynchronous
                });

            session = await JsonSerializer.DeserializeAsync(
                stream,
                NitroJsonContext.Default.NitroSession,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            return (Unusable($"could not be parsed: {ex.Message}"), true);
        }
        catch (IOException ex)
        {
            return (Unusable($"could not be read: {ex.Message}"), true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (Unusable($"could not be read: {ex.Message}"), false);
        }

        if (session is null)
        {
            return (Unusable("is empty."), false);
        }

        if (string.IsNullOrWhiteSpace(session.Tokens?.AccessToken))
        {
            return (Unusable("carries no access token."), false);
        }

        if (session.Tokens.ExpiresAt is null)
        {
            return (Unusable("carries no access token expiry."), false);
        }

        return (
            new NitroSessionReadResult(NitroSessionStatus.Available, session, Message: null),
            false);
    }

    private NitroSessionReadResult Unusable(string reason)
        => new(
            NitroSessionStatus.Unusable,
            Session: null,
            $"The Nitro session file '{_sessionFilePath}' {reason}");
}
