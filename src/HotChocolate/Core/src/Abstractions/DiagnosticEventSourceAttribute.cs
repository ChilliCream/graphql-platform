namespace HotChocolate;

/// <summary>
/// This attribute can be used by custom diagnostic event listeners
/// to specify the source to which a listener shall be bound to.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DiagnosticEventSourceAttribute : Attribute
{
    /// <summary>
    /// This attribute can be used by custom diagnostic event listeners
    /// to specify the source to which a listener shall be bound to.
    /// </summary>
    /// <param name="listener">
    /// The listener interface.
    /// </param>
    public DiagnosticEventSourceAttribute(Type listener)
    {
        Listener = listener;
    }

    /// <summary>
    /// Gets the listener interface.
    /// </summary>
    public Type Listener { get; }
}
