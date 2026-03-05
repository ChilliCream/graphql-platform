using System.Buffers;

namespace Mocha;

/// <summary>
/// Represents the context available during message consumption, combining message metadata with
/// execution capabilities.
/// </summary>
public interface IConsumeContext : IMessageContext, IExecutionContext;
