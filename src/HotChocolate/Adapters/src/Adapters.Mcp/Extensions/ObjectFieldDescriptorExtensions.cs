using HotChocolate.Adapters.Mcp.Directives;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Adapters.Mcp.Extensions;

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

        descriptor.ExtendWith(extension =>
        {
            var typeRef = extension.Context.TypeInspector.GetTypeRef(typeof(McpToolAnnotationsDirectiveType));
            extension.Configuration.Dependencies.Add(new TypeDependency(typeRef));
        });

        return descriptor.Directive(
            new McpToolAnnotationsDirective
            {
                DestructiveHint = destructiveHint,
                IdempotentHint = idempotentHint,
                OpenWorldHint = openWorldHint
            });
    }
}
