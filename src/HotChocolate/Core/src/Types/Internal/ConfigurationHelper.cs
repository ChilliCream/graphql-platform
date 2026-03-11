using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;

namespace HotChocolate.Internal;

/// <summary>
/// Provides a set of utilities to for configuration objects.
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Adds a directive to a configuration that implements <see cref="IDirectiveConfigurationProvider"/>.
    /// </summary>
    /// <param name="directivesContainer">
    /// The configuration.
    /// </param>
    /// <param name="directive">
    /// The directive that shall be added.
    /// </param>
    /// <param name="typeInspector">
    /// The type inspector from the <see cref="IDescriptorContext"/>.
    /// </param>
    /// <typeparam name="T">
    /// The type of the directive.
    /// </typeparam>
    public static void AddDirective<T>(
        this IDirectiveConfigurationProvider directivesContainer,
        T directive,
        ITypeInspector typeInspector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(directivesContainer);
        ArgumentNullException.ThrowIfNull(directive);
        ArgumentNullException.ThrowIfNull(typeInspector);

        switch (directive)
        {
            case DirectiveNode node:
                directivesContainer.Directives.Add(
                    new DirectiveConfiguration(node));
                break;

            case string directiveName:
                AddDirective(directivesContainer, directiveName);
                break;

            default:
                directivesContainer.Directives.Add(
                    new DirectiveConfiguration(
                        directive,
                        TypeReference.CreateDirective(typeInspector.GetType(directive.GetType()))));
                break;
        }
    }

    /// <summary>
    /// Adds a directive to a configuration that implements <see cref="IDirectiveConfigurationProvider"/>.
    /// </summary>
    /// <param name="directivesContainer">
    /// The configuration.
    /// </param>
    /// <param name="directiveName">
    /// The name of the directive.
    /// </param>
    /// <param name="arguments">
    /// The arguments of the directive.
    /// </param>
    public static void AddDirective(
        this IDirectiveConfigurationProvider directivesContainer,
        string directiveName,
        params IEnumerable<ArgumentNode> arguments)
    {
        ArgumentNullException.ThrowIfNull(directivesContainer);
        ArgumentException.ThrowIfNullOrEmpty(directiveName);
        ArgumentNullException.ThrowIfNull(arguments);

        directivesContainer.Directives.Add(
            new DirectiveConfiguration(
                new DirectiveNode(
                    directiveName.EnsureGraphQLName(),
                    [.. arguments])));
    }

    /// <summary>
    /// Applies <paramref name="configurations"/> to a descriptor.
    /// </summary>
    /// <param name="context">
    /// The descriptor context.
    /// </param>
    /// <param name="descriptor">
    /// The descriptor to which the configurations shall be applied.
    /// </param>
    /// <param name="attributeProvider">
    /// The attribute provider that provided the configurations.
    /// </param>
    /// <param name="configurations">
    /// The descriptor configurations.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The attribute provider is required but not set.
    /// </exception>
    public static void ApplyConfiguration(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider? attributeProvider,
        params ReadOnlySpan<IDescriptorConfiguration> configurations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (configurations.IsEmpty)
        {
            return;
        }

        foreach (var configuration in configurations)
        {
            if (configuration.RequiresAttributeProvider && attributeProvider is null)
            {
                throw new InvalidOperationException("The attribute provider is not configured.");
            }

            configuration.TryConfigure(context, descriptor, attributeProvider);
        }
    }
}
