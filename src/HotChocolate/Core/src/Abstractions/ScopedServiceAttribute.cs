using System;

namespace HotChocolate;

/// <summary>
/// Marks a resolver parameter as a pooled service that shall be injected by the execution engine.
/// </summary>
// TODO : Mark obsolete with 13
// [Obsolete("Use [Service(ServiceKind.Pooled)]")]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ScopedServiceAttribute : Attribute { }
