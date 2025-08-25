using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ModelContextProtocol.Directives;

/// <summary>
/// Additional properties describing a Tool to clients.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpToolAnnotationsAttribute : DescriptorAttribute
{
    private readonly bool? _destructiveHint;
    private readonly bool? _idempotentHint;
    private readonly bool? _openWorldHint;

    /// <summary>
    /// If <c>true</c>, the tool may perform destructive updates to its environment. If
    /// <c>false</c>, the tool performs only additive updates.
    /// </summary>
    public bool DestructiveHint
    {
        get => _destructiveHint ?? throw new InvalidOperationException();
        init => _destructiveHint = value;
    }

    /// <summary>
    /// If <c>true</c>, calling the tool repeatedly with the same arguments will have no additional
    /// effect on its environment.
    /// </summary>
    public bool IdempotentHint
    {
        get => _idempotentHint ?? throw new InvalidOperationException();
        init => _idempotentHint = value;
    }

    /// <summary>
    /// If <c>true</c>, this tool may interact with an “open world” of external entities. If
    /// <c>false</c>, the tool’s domain of interaction is closed. For example, the world of a web
    /// search tool is open, whereas that of a memory tool is not.
    /// </summary>
    public bool OpenWorldHint
    {
        get => _openWorldHint ?? throw new InvalidOperationException();
        init => _openWorldHint = value;
    }

    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if (descriptor is IObjectFieldDescriptor objectFieldDescriptor)
        {
            objectFieldDescriptor.Directive(
                new McpToolAnnotationsDirective
                {
                    DestructiveHint = _destructiveHint,
                    IdempotentHint = _idempotentHint,
                    OpenWorldHint = _openWorldHint
                });
        }
    }
}
