namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves <see cref="LaunchDescriptor"/> for the currently running
/// process.
/// </summary>
internal interface ILaunchDescriptorResolver
{
    LaunchDescriptor Resolve();
}
