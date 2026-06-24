using System.Reflection;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Composite;

/// <summary>
/// <para>
/// Applies the @subscribe directive to this subscription field to declare that it is
/// fulfilled by an event stream behind the distributed GraphQL executor.
/// </para>
/// <para>
/// @subscribe(message: "user { id }", topics: ["onUserCreated"], broker: "kafka")
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EventStreamAttribute : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStreamAttribute"/> class.
    /// </summary>
    /// <param name="message">
    /// The payload selection set. The outer braces are optional and will be added if not present.
    /// </param>
    public EventStreamAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the payload selection set.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets or sets the topics the event stream subscribes to.
    /// </summary>
    public string[]? Topics { get; set; }

    /// <summary>
    /// Gets or sets a single topic the event stream subscribes to.
    /// </summary>
    public string? Topic
    {
        get => Topics is { Length: > 0 } ? Topics[0] : null;
        set => Topics = value is null ? null : [value];
    }

    /// <summary>
    /// Gets or sets the broker that provides the event stream.
    /// </summary>
    public string? Broker { get; set; }

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
        => descriptor.EventStream(Message, Topics, Broker);
}
