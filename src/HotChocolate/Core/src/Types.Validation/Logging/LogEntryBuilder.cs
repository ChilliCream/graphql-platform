using HotChocolate.Types;

namespace HotChocolate.Logging;

/// <summary>
/// A builder for <see cref="LogEntry"/> instances.
/// </summary>
public sealed class LogEntryBuilder
{
    private readonly LogEntry _logEntry = new();

    /// <summary>
    /// Creates a new instance of <see cref="LogEntryBuilder"/>.
    /// </summary>
    public static LogEntryBuilder New() => new();

    /// <summary>
    /// Sets the message of the log entry.
    /// </summary>
    /// <param name="format">The message format.</param>
    /// <param name="args">The message arguments.</param>
    /// <returns>The log entry builder.</returns>
    public LogEntryBuilder SetMessage(string format, params object[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        _logEntry.Message = string.Format(format, args);
        return this;
    }

    /// <summary>
    /// Sets the code of the log entry.
    /// </summary>
    /// <param name="code">The code of the log entry.</param>
    /// <returns>The log entry builder.</returns>
    public LogEntryBuilder SetCode(string code)
    {
        _logEntry.Code = code;
        return this;
    }

    /// <summary>
    /// Sets the severity of the log entry.
    /// </summary>
    /// <param name="severity">The severity of the log entry.</param>
    /// <returns>The log entry builder.</returns>
    public LogEntryBuilder SetSeverity(LogSeverity severity)
    {
        _logEntry.Severity = severity;
        return this;
    }

    /// <summary>
    /// Sets the type system member that is associated with this log entry.
    /// </summary>
    /// <param name="typeSystemMember">The type system member.</param>
    /// <returns>The log entry builder.</returns>
    public LogEntryBuilder SetTypeSystemMember(ITypeSystemMember typeSystemMember)
    {
        _logEntry.TypeSystemMember = typeSystemMember;

        if (typeSystemMember is ISchemaCoordinateProvider coordinateProvider)
        {
            _logEntry.Coordinate = coordinateProvider.Coordinate;
        }

        return this;
    }

    /// <summary>
    /// Sets an extension on the log entry.
    /// </summary>
    /// <param name="key">The extension key.</param>
    /// <param name="value">The extension value.</param>
    /// <returns>The log entry builder.</returns>
    public LogEntryBuilder SetExtension(string key, object value)
    {
        _logEntry.Extensions = _logEntry.Extensions.SetItem(key, value);
        return this;
    }

    /// <summary>
    /// Builds the log entry.
    /// </summary>
    /// <returns>The built log entry.</returns>
    public LogEntry Build()
    {
        if (_logEntry.Message is null)
        {
            throw new InvalidOperationException("The message is mandatory.");
        }

        if (_logEntry.Code is null)
        {
            throw new InvalidOperationException("The code is mandatory.");
        }

        if (_logEntry.TypeSystemMember is null)
        {
            throw new InvalidOperationException("The type system member is mandatory.");
        }

        return _logEntry with { };
    }
}
