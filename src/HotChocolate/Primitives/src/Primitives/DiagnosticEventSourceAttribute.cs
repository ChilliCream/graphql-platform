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
    /// <param name="isSchemaService">
    /// Defines if this listener is a schema service.
    /// </param>
    public DiagnosticEventSourceAttribute(Type listener, bool isSchemaService = true)
    {
        ArgumentNullException.ThrowIfNull(listener, nameof(listener));

        Listener = listener;
        IsSchemaService = isSchemaService;
    }

    /// <summary>
    /// Gets the listener interface.
    /// </summary>
    public Type Listener { get; }

    /// <summary>
    /// Gets a value indicating whether this listener is a schema service.
    /// </summary>
    public bool IsSchemaService { get; }
}
