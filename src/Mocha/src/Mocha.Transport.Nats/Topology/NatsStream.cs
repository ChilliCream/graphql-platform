using System.Collections.Immutable;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Represents a JetStream stream, the coarse container that captures a service's subjects.
/// </summary>
public sealed class NatsStream : TopologyResource<NatsStreamConfiguration>, INatsResource
{
    /// <summary>
    /// JetStream's <c>err_code</c> for a stream that does not exist.
    /// </summary>
    private const int StreamNotFoundErrorCode = 10059;

    /// <summary>
    /// Gets the name of this stream as declared in JetStream.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the subjects this stream captures.
    /// </summary>
    public ImmutableArray<string> Subjects { get; private set; } = [];

    /// <inheritdoc />
    public bool? AutoProvision { get; private set; }

    /// <summary>
    /// Gets the deduplication window applied to the <c>Nats-Msg-Id</c> header, or
    /// <see cref="TimeSpan.Zero"/> when the server's own default applies.
    /// </summary>
    public TimeSpan DuplicateWindow { get; private set; }

    /// <summary>
    /// Gets a value indicating whether per-message TTL headers are honoured.
    /// </summary>
    public bool AllowMsgTtl { get; private set; }

    /// <summary>
    /// Gets a value indicating whether message scheduling is enabled.
    /// </summary>
    public bool AllowMsgSchedules { get; private set; }

    private StreamConfig _config = null!;

    /// <inheritdoc />
    protected override void OnInitialize(NatsStreamConfiguration configuration)
    {
        Name = configuration.Name ?? throw new InvalidOperationException("Stream name is required.");

        if (!NatsNaming.IsValidName(Name))
        {
            throw new InvalidOperationException(
                $"'{Name}' is not a valid JetStream stream name. Stream names cannot contain "
                + "'.', '*', '>', whitespace or path separators.");
        }

        Subjects = [.. configuration.Subjects ?? []];
        AutoProvision = configuration.AutoProvision;
        DuplicateWindow = configuration.DuplicateWindow ?? TimeSpan.Zero;
        AllowMsgTtl = configuration.AllowMsgTtl ?? false;
        AllowMsgSchedules = configuration.AllowMsgSchedules ?? false;

        _config = new StreamConfig
        {
            Name = Name,
            Subjects = [.. Subjects],
            Retention = configuration.Retention ?? StreamConfigRetention.Limits,
            Storage = configuration.Storage ?? StreamConfigStorage.File,
            DuplicateWindow = DuplicateWindow,
            AllowMsgTTL = AllowMsgTtl,
            AllowMsgSchedules = AllowMsgSchedules
        };

        if (configuration.MaxAge is { } maxAge)
        {
            _config.MaxAge = maxAge;
        }

        if (configuration.MaxMsgs is { } maxMsgs)
        {
            _config.MaxMsgs = maxMsgs;
        }

        if (configuration.MaxBytes is { } maxBytes)
        {
            _config.MaxBytes = maxBytes;
        }

        if (configuration.NumReplicas is { } numReplicas)
        {
            _config.NumReplicas = numReplicas;
        }
    }

    /// <inheritdoc />
    protected override void OnComplete(NatsStreamConfiguration configuration)
    {
        Address = NatsAddress.ForStream(Topology.Address, Name);
    }

    /// <summary>
    /// Folds a second declaration of this stream into the existing one.
    /// </summary>
    /// <param name="configuration">The configuration to fold in.</param>
    /// <remarks>
    /// Subjects are combined and everything already set is kept, so a declaration wins over the
    /// convention stream that collides with it on retention, storage and limits.
    /// </remarks>
    internal void Merge(NatsStreamConfiguration configuration)
    {
        if (configuration.Subjects is { Count: > 0 } incoming)
        {
            Subjects = [.. SubjectMatcher.Collapse(Subjects.Concat(incoming))];
            _config.Subjects = [.. Subjects];
        }

        AutoProvision ??= configuration.AutoProvision;

        // Only ever turned on: a stream that has to hold scheduled or expiring messages for one
        // caller must keep doing so for the other.
        if (configuration.AllowMsgTtl == true)
        {
            AllowMsgTtl = true;
            _config.AllowMsgTTL = true;
        }

        if (configuration.AllowMsgSchedules == true)
        {
            AllowMsgSchedules = true;
            _config.AllowMsgSchedules = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask ProvisionAsync(INatsJSContext context, CancellationToken cancellationToken)
    {
        if (Origin is TopologyOrigin.Convention)
        {
            await AdoptExistingConfigurationAsync(context, cancellationToken);
        }

        await context.CreateOrUpdateStreamAsync(_config, cancellationToken);
    }

    /// <summary>
    /// Rebases this stream on the configuration the server already holds, adding its own subjects to
    /// it rather than replacing anything.
    /// </summary>
    /// <param name="context">The JetStream context used to query the server.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// Keeps the subjects and settings the owning service chose, since an update sends the whole
    /// configuration. Convention streams only; a declaration stays authoritative.
    /// </remarks>
    private async ValueTask AdoptExistingConfigurationAsync(
        INatsJSContext context,
        CancellationToken cancellationToken)
    {
        INatsJSStream existing;

        try
        {
            existing = await context.GetStreamAsync(Name, cancellationToken: cancellationToken);
        }
        catch (NatsJSApiException exception) when (exception.Error.ErrCode == StreamNotFoundErrorCode)
        {
            return;
        }

        var adopted = existing.Info.Config;
        var subjects = SubjectMatcher.Collapse((adopted.Subjects ?? []).Concat(Subjects));

        adopted.Subjects = [.. subjects];

        // Only ever turned on, never off: a peer bound to this stream may depend on them.
        adopted.AllowMsgTTL = adopted.AllowMsgTTL || AllowMsgTtl;
        adopted.AllowMsgSchedules = adopted.AllowMsgSchedules || AllowMsgSchedules;

        _config = adopted;

        Subjects = [.. subjects];
        DuplicateWindow = adopted.DuplicateWindow;
        AllowMsgTtl = adopted.AllowMsgTTL;
        AllowMsgSchedules = adopted.AllowMsgSchedules;
    }
}
