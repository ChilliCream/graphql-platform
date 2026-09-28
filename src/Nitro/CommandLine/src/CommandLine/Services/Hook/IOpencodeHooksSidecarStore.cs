namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal interface IOpencodeHooksSidecarStore
{
    Task<(OpencodeHooksSidecarFile File, string Hash)> ReadWithHashAsync(CancellationToken cancellationToken);

    Task<bool> WriteIfUnchangedAsync(
        OpencodeHooksSidecarFile file,
        string hashAtRead,
        CancellationToken cancellationToken);
}
