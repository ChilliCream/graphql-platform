using HotChocolate.ModelContextProtocol.Directives;
using HotChocolate.Types;

namespace HotChocolate.ModelContextProtocol.Extensions;

/// <summary>
/// Provides extension methods to <see cref="IObjectFieldDescriptor"/>.
/// </summary>
public static class ObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Additional properties describing a Tool to clients.
    /// </summary>
    public static IObjectFieldDescriptor McpToolAnnotations(
        this IObjectFieldDescriptor descriptor,
        bool? destructiveHint = null,
        bool? idempotentHint = null,
        bool? openWorldHint = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Directive(
            new McpToolAnnotationsDirective
            {
                DestructiveHint = destructiveHint,
                IdempotentHint = idempotentHint,
                OpenWorldHint = openWorldHint
            });
    }
}
