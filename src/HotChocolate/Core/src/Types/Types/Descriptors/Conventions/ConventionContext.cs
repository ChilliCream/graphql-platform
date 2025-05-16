#nullable enable

using HotChocolate.Features;

namespace HotChocolate.Types.Descriptors;

/// <summary>
/// The convention context is available during the convention initialization process.
/// </summary>
/// <param name="scope">
/// The scope of the convention.
/// </param>
/// <param name="services">
/// The services.
/// </param>
/// <param name="descriptorContext">
/// The descriptor context.
/// </param>
internal sealed class ConventionContext(
    string? scope,
    IServiceProvider services,
    IDescriptorContext descriptorContext)
    : IConventionContext
{
    /// <inheritdoc />
    public string? Scope { get; } = scope;

    /// <inheritdoc />
    public IServiceProvider Services { get; } = services;

    /// <inheritdoc />
    public IFeatureCollection Features => DescriptorContext.Features;

    /// <inheritdoc />
    public IDescriptorContext DescriptorContext { get; } = descriptorContext;

    /// <summary>
    /// Creates a new convention context.
    /// </summary>
    /// <param name="scope">
    /// The scope of the convention.
    /// </param>
    /// <param name="services">
    /// The services.
    /// </param>
    /// <param name="descriptorContext">
    /// The descriptor context.
    /// </param>
    /// <returns></returns>
    public static ConventionContext Create(
        string? scope,
        IServiceProvider services,
        IDescriptorContext descriptorContext)
        => new(scope, services, descriptorContext);
}
