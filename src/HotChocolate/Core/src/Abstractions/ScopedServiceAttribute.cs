using System;

namespace HotChocolate;

/// <summary>
/// Marks a resolver parameter as a pooled service that shall be injected by the execution engine.
/// </summary>
[Obsolete("Use [Service(ServiceKind.Pooled)] or [Service(ServiceKind.Resolver)]")]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ScopedServiceAttribute : Attribute;
