using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents a single subscription event, pairing the event result with the
/// <see cref="IMemoryArena"/> that backs its document. Yielding a
/// <see cref="SourceSchemaEventResult"/> transfers ownership of <see cref="Arena"/> to the consumer,
/// which is then responsible for its lifetime.
/// </summary>
/// <param name="Arena">The arena that backs the document of <paramref name="Result"/>.</param>
/// <param name="Result">The event result.</param>
public readonly record struct SourceSchemaEventResult(IMemoryArena Arena, SourceSchemaResult Result);
