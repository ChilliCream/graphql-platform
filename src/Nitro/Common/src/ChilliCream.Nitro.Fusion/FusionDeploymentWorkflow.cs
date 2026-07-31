using System.Security.Cryptography;
using ChilliCream.Nitro.Fusion.Transport;

namespace ChilliCream.Nitro.Fusion;

internal sealed class FusionDeploymentWorkflow(
    IFusionDeploymentTransportFactory transportFactory)
    : IFusionDeploymentWorkflow
{
    public async Task<FusionSourceSchemaDownload?> DownloadSourceSchemaAsync(
        FusionTarget target,
        FusionSourceSchemaVersion source,
        CancellationToken cancellationToken)
    {
        ValidateTarget(target);
        ValidateSourceVersion(source);

        await using var transport = await transportFactory.OpenAsync(
            target,
            cancellationToken);
        var archive = await transport.DownloadSourceSchemaAsync(
            source.Name,
            source.Version,
            cancellationToken);

        if (archive is null)
        {
            return null;
        }

        await using var stream = new MemoryStream(archive, writable: false);
        var content = await FusionArchiveContent.ReadAsync(
            stream,
            source.Name,
            cancellationToken);
        var contentSha256 = content.ComputeSha256(cancellationToken);

        return new FusionSourceSchemaDownload(
            source.Name,
            source.Version,
            archive,
            contentSha256);
    }

    public async Task ReconcileSourceSchemaAsync(
        FusionTarget target,
        FusionSourceSchemaUpload source,
        CancellationToken cancellationToken)
    {
        ValidateTarget(target);
        ValidateSource(source);

        var localContent = await FusionArchiveContent.ReadAsync(
            source.ArchivePath,
            source.Name,
            cancellationToken);
        var localContentSha256 = localContent.ComputeSha256(cancellationToken);
        await VerifySha256Async(source, cancellationToken);

        await using var transport = await transportFactory.OpenAsync(
            target,
            cancellationToken);
        var remoteArchive = await transport.DownloadSourceSchemaAsync(
            source.Name,
            source.Version,
            cancellationToken);

        if (remoteArchive is not null)
        {
            await EnsureContentMatchesAsync(
                source,
                localContentSha256,
                remoteArchive,
                cancellationToken);
            return;
        }

        FusionRemoteCommandResult uploadResult;
        try
        {
            uploadResult = await transport.UploadSourceSchemaAsync(
                source.Version,
                source.ArchivePath,
                cancellationToken);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ReconcileAfterUncertainUploadAsync(
                transport,
                source,
                localContentSha256,
                exception,
                cancellationToken);
            return;
        }

        if (uploadResult.Succeeded)
        {
            return;
        }

        if (uploadResult.IsDuplicate)
        {
            var racedArchive = await transport.DownloadSourceSchemaAsync(
                source.Name,
                source.Version,
                cancellationToken);
            if (racedArchive is null)
            {
                throw new FusionIndeterminateStateException(
                    $"Nitro reported source schema '{source.Name}' version "
                    + $"'{source.Version}' as duplicated, but the version "
                    + "could not be read back.");
            }

            await EnsureContentMatchesAsync(
                source,
                localContentSha256,
                racedArchive,
                cancellationToken);
            return;
        }

        throw CreateRemoteFailure(
            "Nitro rejected the Fusion source schema upload.",
            uploadResult.Errors);
    }

    public async Task PublishAsync(
        FusionPublicationRequest request,
        string fusionArchivePath,
        CancellationToken cancellationToken)
    {
        ValidatePublication(request, fusionArchivePath);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        timeout.CancelAfter(request.OperationTimeout);
        var operationToken = timeout.Token;
        string? requestId = null;

        try
        {
            await using var transport = await transportFactory.OpenAsync(
                request.Target,
                operationToken);
            FusionRemoteBeginResult begin;

            try
            {
                begin = await transport.BeginPublishAsync(
                    request,
                    operationToken);
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException)
            {
                throw new FusionIndeterminateStateException(
                    "The Fusion publication request may have been created, "
                    + "but Nitro did not return a verifiable result.",
                    exception);
            }

            if (begin.Errors.Count > 0)
            {
                throw CreateRemoteFailure(
                    "Nitro rejected the Fusion publication request.",
                    begin.Errors);
            }

            requestId = begin.RequestId;
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new FusionIndeterminateStateException(
                    "Nitro accepted the Fusion publication request without "
                    + "returning a request identifier.");
            }

            await using var events = transport.WatchPublishAsync(
                    requestId,
                    operationToken)
                .GetAsyncEnumerator(operationToken);

            var readyResult = await WaitForReadyAsync(
                events,
                requestId,
                operationToken);
            if (readyResult is PublishWaitResult.Succeeded)
            {
                return;
            }

            await EnsureRemoteCommandSucceededAsync(
                () => transport.ClaimPublishAsync(requestId, operationToken),
                "claim the Fusion publication slot",
                requestId);

            if (!request.WaitForApproval)
            {
                await EnsureRemoteCommandSucceededAsync(
                    () => transport.ValidatePublishAsync(
                        requestId,
                        fusionArchivePath,
                        operationToken),
                    "validate the Fusion configuration",
                    requestId);

                var validation = await WaitForValidationAsync(
                    events,
                    requestId,
                    operationToken);
                if (validation is { Succeeded: false } && !request.Force)
                {
                    await ReleaseKnownFailedValidationAsync(
                        transport,
                        requestId,
                        operationToken);
                    throw CreateRemoteFailure(
                        "Nitro rejected the Fusion configuration validation.",
                        validation.Errors);
                }
            }

            await EnsureRemoteCommandSucceededAsync(
                () => transport.CommitPublishAsync(
                    requestId,
                    fusionArchivePath,
                    operationToken),
                "commit the Fusion configuration",
                requestId);

            await WaitForTerminalPublishAsync(
                events,
                requestId,
                timeout,
                request.ApprovalTimeout,
                operationToken);
        }
        catch (OperationCanceledException) when (
            !cancellationToken.IsCancellationRequested)
        {
            throw new FusionIndeterminateStateException(
                "The Fusion publication timed out before a terminal result "
                + "could be verified.",
                requestId);
        }
    }

    private static async Task ReconcileAfterUncertainUploadAsync(
        IFusionDeploymentTransport transport,
        FusionSourceSchemaUpload source,
        string localContentSha256,
        Exception uploadException,
        CancellationToken cancellationToken)
    {
        byte[]? remoteArchive;
        try
        {
            remoteArchive = await transport.DownloadSourceSchemaAsync(
                source.Name,
                source.Version,
                cancellationToken);
        }
        catch (Exception readException)
        {
            throw new FusionIndeterminateStateException(
                $"The upload of source schema '{source.Name}' version "
                + $"'{source.Version}' may have succeeded and could not be "
                + "verified.",
                new AggregateException(uploadException, readException));
        }

        if (remoteArchive is null)
        {
            throw new FusionIndeterminateStateException(
                $"The upload of source schema '{source.Name}' version "
                + $"'{source.Version}' returned an uncertain result and the "
                + "version was not available for verification.",
                uploadException);
        }

        await EnsureContentMatchesAsync(
            source,
            localContentSha256,
            remoteArchive,
            cancellationToken);
    }

    private static async Task EnsureContentMatchesAsync(
        FusionSourceSchemaUpload source,
        string localContentSha256,
        byte[] remoteArchive,
        CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(remoteArchive, writable: false);
        var remoteContent = await FusionArchiveContent.ReadAsync(
            stream,
            source.Name,
            cancellationToken);

        if (!string.Equals(
            localContentSha256,
            remoteContent.ComputeSha256(cancellationToken),
            StringComparison.Ordinal))
        {
            throw new FusionIdentityCollisionException(
                $"Source schema '{source.Name}' version '{source.Version}' "
                + "already exists with different normalized schema, settings, "
                + "or extensions.");
        }
    }

    private static async Task VerifySha256Async(
        FusionSourceSchemaUpload source,
        CancellationToken cancellationToken)
    {
        ValidateSha256(source.Sha256, "The source schema SHA-256");

        await using var stream = File.OpenRead(source.ArchivePath);
        var actual = Convert.ToHexString(
            await SHA256.HashDataAsync(stream, cancellationToken));
        if (!string.Equals(
            actual,
            source.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new FusionDeploymentException(
                $"The SHA-256 for source schema '{source.Name}' version "
                + $"'{source.Version}' does not match its archive.");
        }
    }

    private static async Task<PublishWaitResult> WaitForReadyAsync(
        IAsyncEnumerator<FusionRemoteEvent> events,
        string requestId,
        CancellationToken cancellationToken)
    {
        while (await MoveNextAsync(events, requestId, cancellationToken))
        {
            switch (events.Current.Kind)
            {
                case FusionRemoteEventKind.Ready:
                    return PublishWaitResult.Ready;
                case FusionRemoteEventKind.PublishingSucceeded:
                    return PublishWaitResult.Succeeded;
                case FusionRemoteEventKind.PublishingFailed:
                    throw CreateRemoteFailure(
                        "The Fusion publication failed.",
                        events.Current.Errors);
                case FusionRemoteEventKind.Queued:
                    continue;
                default:
                    throw new FusionIndeterminateStateException(
                        "The Fusion publication request was already in an "
                        + $"ambiguous '{events.Current.Kind}' state.",
                        requestId);
            }
        }

        throw CreateEndedEarly(requestId);
    }

    private static async Task<FusionValidationResult> WaitForValidationAsync(
        IAsyncEnumerator<FusionRemoteEvent> events,
        string requestId,
        CancellationToken cancellationToken)
    {
        while (await MoveNextAsync(events, requestId, cancellationToken))
        {
            switch (events.Current.Kind)
            {
                case FusionRemoteEventKind.ValidationSucceeded:
                    return new FusionValidationResult(true, []);
                case FusionRemoteEventKind.ValidationFailed:
                    return new FusionValidationResult(
                        false,
                        events.Current.Errors);
                case FusionRemoteEventKind.InProgress:
                    continue;
                case FusionRemoteEventKind.PublishingFailed:
                    throw CreateRemoteFailure(
                        "The Fusion publication failed during validation.",
                        events.Current.Errors);
                default:
                    throw new FusionIndeterminateStateException(
                        "Nitro reported an unexpected Fusion validation state "
                        + $"'{events.Current.Kind}'.",
                        requestId);
            }
        }

        throw CreateEndedEarly(requestId);
    }

    private static async Task WaitForTerminalPublishAsync(
        IAsyncEnumerator<FusionRemoteEvent> events,
        string requestId,
        CancellationTokenSource timeout,
        TimeSpan approvalTimeout,
        CancellationToken cancellationToken)
    {
        while (await MoveNextAsync(events, requestId, cancellationToken))
        {
            switch (events.Current.Kind)
            {
                case FusionRemoteEventKind.PublishingSucceeded:
                    return;
                case FusionRemoteEventKind.PublishingFailed:
                    throw CreateRemoteFailure(
                        "The Fusion publication failed.",
                        events.Current.Errors);
                case FusionRemoteEventKind.WaitingForApproval:
                    timeout.CancelAfter(approvalTimeout);
                    continue;
                case FusionRemoteEventKind.Approved:
                case FusionRemoteEventKind.InProgress:
                case FusionRemoteEventKind.ValidationSucceeded:
                    continue;
                default:
                    throw new FusionIndeterminateStateException(
                        "Nitro reported an unexpected Fusion publishing state "
                        + $"'{events.Current.Kind}'.",
                        requestId);
            }
        }

        throw CreateEndedEarly(requestId);
    }

    private static async Task<bool> MoveNextAsync(
        IAsyncEnumerator<FusionRemoteEvent> events,
        string requestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var hasNext = await events.MoveNextAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return hasNext;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new FusionIndeterminateStateException(
                "The Fusion publication event stream ended with an error.",
                exception,
                requestId);
        }
    }

    private static async Task EnsureRemoteCommandSucceededAsync(
        Func<Task<FusionRemoteCommandResult>> command,
        string operation,
        string requestId)
    {
        FusionRemoteCommandResult result;
        try
        {
            result = await command();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new FusionIndeterminateStateException(
                $"Nitro may have completed the operation to {operation}, but "
                + "its result could not be verified.",
                exception,
                requestId);
        }

        if (!result.Succeeded)
        {
            throw new FusionIndeterminateStateException(
                $"Nitro could not {operation}: "
                + FormatErrors(result.Errors),
                requestId);
        }
    }

    private static async Task ReleaseKnownFailedValidationAsync(
        IFusionDeploymentTransport transport,
        string requestId,
        CancellationToken cancellationToken)
    {
        var result = await transport.ReleasePublishAsync(
            requestId,
            cancellationToken);
        if (!result.Succeeded)
        {
            throw new FusionIndeterminateStateException(
                "The failed Fusion validation was known, but its publication "
                + "slot could not be released: "
                + FormatErrors(result.Errors),
                requestId);
        }
    }

    private static FusionIndeterminateStateException CreateEndedEarly(
        string requestId)
        => new(
            "The Fusion publication event stream ended before Nitro reported "
            + "a terminal result.",
            requestId);

    private static FusionDeploymentException CreateRemoteFailure(
        string message,
        IReadOnlyList<string> errors)
        => new($"{message} {FormatErrors(errors)}");

    private static string FormatErrors(IReadOnlyList<string> errors)
        => errors.Count is 0
            ? "Nitro returned no error details."
            : string.Join(" ", errors);

    private static void ValidateTarget(FusionTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!target.CloudUrl.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "The Nitro cloud URL must be absolute.",
                nameof(target));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(target.ApiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target.ApiKey);
    }

    private static void ValidateSource(FusionSourceSchemaUpload source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.ArchivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Sha256);
        if (!File.Exists(source.ArchivePath))
        {
            throw new FileNotFoundException(
                "The Fusion source schema archive does not exist.",
                source.ArchivePath);
        }
    }

    private static void ValidateSourceVersion(FusionSourceSchemaVersion source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Version);
    }

    private static void ValidateSha256(string sha256, string description)
    {
        if (sha256.Length is not 64
            || !sha256.All(Uri.IsHexDigit))
        {
            throw new FusionDeploymentException(
                $"{description} must contain exactly 64 hexadecimal "
                + "characters.");
        }
    }

    private static void ValidatePublication(
        FusionPublicationRequest request,
        string fusionArchivePath)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTarget(request.Target);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Stage);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConfigurationTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(fusionArchivePath);
        ArgumentNullException.ThrowIfNull(request.SourceSchemas);

        if (request.SourceSchemas.Count is 0)
        {
            throw new ArgumentException(
                "At least one source schema version must be selected.",
                nameof(request));
        }

        foreach (var source in request.SourceSchemas)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentException.ThrowIfNullOrWhiteSpace(source.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(source.Version);
        }

        if (request.SourceSchemas
            .GroupBy(source => source.Name, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A source schema may only be selected once.",
                nameof(request));
        }

        if (request.OperationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The operation timeout must be greater than zero.");
        }

        if (request.ApprovalTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The approval timeout must be greater than zero.");
        }

        if (!File.Exists(fusionArchivePath))
        {
            throw new FileNotFoundException(
                "The Fusion configuration archive does not exist.",
                fusionArchivePath);
        }

        if (new FileInfo(fusionArchivePath).Length is 0)
        {
            throw new FusionDeploymentException(
                "The Fusion configuration archive is empty.");
        }
    }

    private sealed record FusionValidationResult(
        bool Succeeded,
        IReadOnlyList<string> Errors);

    private enum PublishWaitResult
    {
        Ready,
        Succeeded
    }
}
