namespace Mocha.Shared.Contracts;

/// <summary>
/// Lives in its own namespace on purpose: convention subjects are derived from the message
/// namespace, so a shared contracts namespace is what makes several services claim one subject.
/// </summary>
public sealed record WidgetShipped(string WidgetId);
