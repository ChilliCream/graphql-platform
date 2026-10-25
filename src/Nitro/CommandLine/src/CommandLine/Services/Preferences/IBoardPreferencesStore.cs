using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Services.Preferences;

/// <summary>
/// Reads and writes the per-user board preferences in the global configuration directory.
/// </summary>
internal interface IBoardPreferencesStore
{
    /// <summary>
    /// Reads the saved orientation. A missing, unreadable, or unrecognized value yields
    /// <see cref="BoardOrientation.Auto"/>.
    /// </summary>
    Task<BoardOrientation> ReadOrientationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves <paramref name="orientation"/>. Returns <see langword="false"/> when the file
    /// could not be written.
    /// </summary>
    Task<bool> WriteOrientationAsync(BoardOrientation orientation, CancellationToken cancellationToken);
}
