using HotChocolate.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Adapts a nested <see cref="CompositeResultElement"/> so that it can be written
/// as the "data" payload of an <see cref="IncrementalObjectResult"/>. The incremental
/// delivery contract requires the <c>data</c> value to be the delta to merge at the
/// pending path rather than the fully rooted result, so this formatter writes the
/// subtree element directly instead of the composite result document's root.
/// </summary>
internal sealed class DeferredPayloadDataFormatter : IRawJsonFormatter
{
    private readonly CompositeResultElement _element;

    public DeferredPayloadDataFormatter(CompositeResultElement element)
    {
        _element = element;
    }

    public void WriteDataTo(JsonWriter jsonWriter)
        => _element.WriteTo(jsonWriter);
}
