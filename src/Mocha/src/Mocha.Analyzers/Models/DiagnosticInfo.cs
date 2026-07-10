using Mocha.Analyzers.Utils;

namespace Mocha.Analyzers;

/// <summary>
/// An equatable representation of diagnostic information that can safely
/// flow through the incremental pipeline without rooting Roslyn objects.
/// </summary>
/// <param name="DescriptorId">The diagnostic descriptor ID (e.g., "MO0003").</param>
/// <param name="Location">The source location, or <see langword="null"/> if unavailable.</param>
/// <param name="MessageArgs">The format arguments for the diagnostic message.</param>
public sealed record DiagnosticInfo(
    string DescriptorId,
    LocationInfo? Location,
    ImmutableEquatableArray<string> MessageArgs);
