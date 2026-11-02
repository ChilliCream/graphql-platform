namespace Mocha.Middlewares;

/// <summary>
/// Interface representing host information about the current process and environment.
/// </summary>
public interface IHostInfo : IRemoteHostInfo
{
    /// <summary>
    /// Gets the runtime information.
    /// </summary>
    IRuntimeInfo RuntimeInfo { get; }
}
