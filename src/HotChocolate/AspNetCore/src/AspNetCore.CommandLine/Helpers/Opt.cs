using System.CommandLine;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// This class is a helper class that allows us to create a singleton instance of
/// <see cref="Option"/> 
/// </summary>
internal static class Opt<TOption> where TOption : new()
{
    private static TOption? _instance;

    /// <summary>
    /// Gets the singleton instance of <typeparamref name="TOption"/>
    /// </summary>
    public static TOption Instance { get => _instance ??= new(); }
}
