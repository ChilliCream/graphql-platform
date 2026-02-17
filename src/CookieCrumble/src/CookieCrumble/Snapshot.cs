using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CookieCrumble.Formatters;
using DiffPlex.DiffBuilder;
using static System.Collections.Immutable.ImmutableStack;
using static System.IO.Path;
using ChangeType = DiffPlex.DiffBuilder.Model.ChangeType;

namespace CookieCrumble;

public class Snapshot
{
#if NET10_0_OR_GREATER
    private static readonly Lock s_sync = new();
#else
    private static readonly object s_sync = new();
#endif
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static ImmutableStack<ISnapshotValueFormatter> s_formatters =
        CreateRange(new ISnapshotValueFormatter[]
        {
            new PlainTextSnapshotValueFormatter(),
            new ExceptionSnapshotValueFormatter(),
            new HttpResponseSnapshotValueFormatter(),
            new JsonDocumentSnapshotValueFormatter(),
            new JsonElementSnapshotValueFormatter()
        });
    private static readonly JsonSnapshotValueFormatter s_defaultFormatter = new();

    private static ITestFramework s_testFramework = null!;
    private readonly List<ISnapshotSegment> _segments = [];
    private readonly string _title;
    private readonly string _fileName;
    private string _extension;
    private string? _postFix;

    public Snapshot(string? postFix = null, string? extension = null)
    {
        if (s_testFramework is null)
        {
            throw new Exception("Please initialize a test framework before using Snapshot");
        }

        var frames = new StackTrace(true).GetFrames();
        _title = CreateMarkdownTitle(frames);
        _fileName = CreateFileName(frames);
        _postFix = postFix;
        _extension = extension ?? ".snap";
    }

    public string Title => _title;

    public static Snapshot Create(string? postFix = null, string? extension = null)
        => new(postFix, extension);

    public static DisposableSnapshot Start(string? postFix = null, string? extension = null)
        => new(postFix, extension);

    public static void Match(
        object? value,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value, formatter: formatter);
        snapshot.Match();
    }

    public static void Match(
        object? value1,
        object? value2,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value1, formatter: formatter);
        snapshot.Add(value2, formatter: formatter);
        snapshot.Match();
    }

    public static void Match(
        object? value1,
        object? value2,
        object? value3,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value1, formatter: formatter);
        snapshot.Add(value2, formatter: formatter);
        snapshot.Add(value3, formatter: formatter);
        snapshot.Match();
    }

    public static void RegisterTestFramework(
        ITestFramework testFramework)
    {
        ArgumentNullException.ThrowIfNull(testFramework);

        lock (s_sync)
        {
            s_testFramework = testFramework;
        }
    }

    public static void RegisterFormatter(
        ISnapshotValueFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        lock (s_sync)
        {
            s_formatters = s_formatters.Push(formatter);
        }
    }

    public static void TryRegisterFormatter(
        ISnapshotValueFormatter formatter,
        bool typeCheck = true)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        lock (s_sync)
        {
            if (typeCheck)
            {
                var type = formatter.GetType();
                if (s_formatters.Any(t => t.GetType() == type))
                {
                    return;
                }
            }
            else
            {
                if (s_formatters.Contains(formatter))
                {
                    return;
                }
            }

            s_formatters = s_formatters.Push(formatter);
        }
    }

    public Snapshot Clear()
    {
        _segments.Clear();
        return this;
    }

    public Snapshot Add(
        object? value,
        string? name = null,
        ISnapshotValueFormatter? formatter = null)
    {
        formatter ??= FindSerializer(value);
        _segments.Add(new SnapshotSegment(name, value, formatter));
        return this;
    }

    public Snapshot Add(
        object? value,
        string name,
        string markdownLanguage)
    {
        _segments.Add(
            new SnapshotSegment(
                name,
                value,
                new PlainTextSnapshotValueFormatter(markdownLanguage)));

        return this;
    }

    public Snapshot Add(SnapshotValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _segments.Add(value);
        return this;
    }

    public Snapshot SetExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(extension);

        _extension = extension;
        return this;
    }

    public Snapshot SetPostFix(string postFix)
    {
        ArgumentException.ThrowIfNullOrEmpty(postFix);

        _postFix = postFix;
        return this;
    }

    private static ISnapshotValueFormatter FindSerializer(object? value)
    {
        // we capture the current immutable serializer list
        var serializers = s_formatters;

        // we iterate over the captured stack.
        foreach (var serializer in serializers)
        {
            if (serializer.CanHandle(value))
            {
                return serializer;
            }
        }

        return s_defaultFormatter;
    }

    public async ValueTask MatchAsync(CancellationToken cancellationToken = default)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);
        EnsureEndOfBufferNewline(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            CheckStrictMode();
            EnsureDirectoryExists(snapshotFile);
            await using var stream = File.Create(snapshotFile);
            await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);

            var before = await File.ReadAllTextAsync(snapshotFile, cancellationToken);
            var after = s_utf8.GetString(writer.WrittenSpan);

            if (!MatchSnapshot(before, after, false, out var diff))
            {
                EnsureDirectoryExists(mismatchFile);
                await using var stream = File.Create(mismatchFile);
                await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
                s_testFramework.ThrowTestException(diff);
            }
        }
    }

    public string Match()
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);
        EnsureEndOfBufferNewline(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            CheckStrictMode();
            EnsureDirectoryExists(snapshotFile);
            using var stream = File.Create(snapshotFile);
            stream.Write(writer.WrittenSpan);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);
            var before = File.ReadAllText(snapshotFile);
            var after = s_utf8.GetString(writer.WrittenSpan);

            if (!MatchSnapshot(before, after, false, out var diff))
            {
                EnsureDirectoryExists(mismatchFile);
                using var stream = File.Create(mismatchFile);
                stream.Write(writer.WrittenSpan);
                s_testFramework.ThrowTestException(diff);
            }
        }

        return s_utf8.GetString(writer.WrittenSpan);
    }

    public async ValueTask MatchMarkdownAsync(CancellationToken cancellationToken = default)
    {
        var writer = new ArrayBufferWriter<byte>();

        writer.Append($"# {_title}");
        writer.AppendLine();
        writer.AppendLine();

        WriteMarkdownSegments(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateMarkdownSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            CheckStrictMode();
            EnsureDirectoryExists(snapshotFile);
            await using var stream = File.Create(snapshotFile);
            await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateMarkdownSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);
            var before = await File.ReadAllTextAsync(snapshotFile, cancellationToken);
            var after = s_utf8.GetString(writer.WrittenSpan);

            if (MatchSnapshot(before, after, false, out var diff))
            {
                return;
            }

            EnsureDirectoryExists(mismatchFile);
            await using var stream = File.Create(mismatchFile);
            await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
            s_testFramework.ThrowTestException(diff);
        }
    }

    public void MatchMarkdown()
    {
        var writer = new ArrayBufferWriter<byte>();

        writer.Append($"# {_title}");
        writer.AppendLine();
        writer.AppendLine();

        WriteMarkdownSegments(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateMarkdownSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            CheckStrictMode();
            EnsureDirectoryExists(snapshotFile);
            using var stream = File.Create(snapshotFile);
            stream.Write(writer.WrittenSpan);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateMarkdownSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);
            var before = File.ReadAllText(snapshotFile);
            var after = s_utf8.GetString(writer.WrittenSpan);

            if (MatchSnapshot(before, after, false, out var diff))
            {
                return;
            }

            EnsureDirectoryExists(mismatchFile);
            using var stream = File.Create(mismatchFile);
            stream.Write(writer.WrittenSpan);
            s_testFramework.ThrowTestException(diff);
        }
    }

    public void MatchInline(string expected)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);

        var after = s_utf8.GetString(writer.WrittenSpan);

        if (!MatchSnapshot(expected, after, true, out var diff))
        {
            s_testFramework.ThrowTestException(diff);
        }
    }

    private void WriteSegments(IBufferWriter<byte> writer)
    {
        if (_segments.Count == 1)
        {
            switch (_segments[0])
            {
                case SnapshotSegment segment:
                    segment.Formatter.Format(writer, segment.Value);
                    break;
                case SnapshotValue value:
                    writer.Write(value.Value);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return;
        }

        var next = false;

        foreach (var segment in _segments)
        {
            if (next)
            {
                writer.AppendLine();
            }

            if (!string.IsNullOrEmpty(segment.Name))
            {
                writer.Append(segment.Name);
                writer.AppendLine();
            }

            writer.AppendSeparator();
            writer.AppendLine();

            switch (segment)
            {
                case SnapshotSegment s:
                    s.Formatter.Format(writer, s.Value);
                    break;
                case SnapshotValue v:
                    writer.Write(v.Value);
                    break;
                default:
                    throw new NotSupportedException();
            }

            writer.AppendLine();
            writer.AppendSeparator();
            writer.AppendLine();

            next = true;
        }
    }

    private void WriteMarkdownSegments(IBufferWriter<byte> writer)
    {
        if (_segments.Count == 1)
        {
            var segment = _segments[0];

            switch (segment)
            {
                case SnapshotSegment s:
                    if (s.Formatter is IMarkdownSnapshotValueFormatter markdownFormatter)
                    {
                        markdownFormatter.FormatMarkdown(writer, s.Value);
                    }
                    else
                    {
                        writer.Append("```text");
                        writer.AppendLine();
                        s.Formatter.Format(writer, s.Value);
                        writer.AppendLine();
                        writer.Append("```");
                        writer.AppendLine();
                    }
                    break;
                case SnapshotValue v:
                    v.FormatMarkdown(writer);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return;
        }

        var i = 0;
        foreach (var segment in _segments)
        {
            i++;
            writer.Append(
                string.IsNullOrEmpty(segment.Name)
                    ? $"## Result {i}"
                    : $"## {segment.Name}");
            writer.AppendLine();
            writer.AppendLine();

            switch (segment)
            {
                case SnapshotSegment s:
                    if (s.Formatter is IMarkdownSnapshotValueFormatter markdownFormatter)
                    {
                        markdownFormatter.FormatMarkdown(writer, s.Value);
                    }
                    else
                    {
                        writer.Append("```text");
                        writer.AppendLine();
                        s.Formatter.Format(writer, s.Value);
                        writer.AppendLine();
                        writer.Append("```");
                        writer.AppendLine();
                    }
                    break;
                case SnapshotValue v:
                    v.FormatMarkdown(writer);
                    break;
                default:
                    throw new NotSupportedException();
            }

            writer.AppendLine();
        }
    }

    private static bool MatchSnapshot(
        string before,
        string after,
        bool inline,
        [NotNullWhen(false)] out string? snapshotDiff)
    {
        if (OperatingSystem.IsWindows())
        {
            // Normalize escaped line endings if the expected value does not explicitly contain them.
            if (!before.Contains(@"\r\n", StringComparison.Ordinal))
            {
                after = after.Replace(@"\r\n", @"\n");
            }
        }

        var diff = InlineDiffBuilder.Diff(before, after);

        if (diff.HasDifferences)
        {
            var output = new StringBuilder();
            output.AppendLine("The snapshot does not match:");

            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        output.Append("+ ");
                        break;

                    case ChangeType.Deleted:
                        output.Append("- ");
                        break;

                    default:
                        output.Append("  ");
                        break;
                }

                output.AppendLine(line.Text);
            }

            if (inline)
            {
                output.AppendLine();
                output.AppendLine("The new snapshot:");
                output.AppendLine(after);
            }

            snapshotDiff = output.ToString();
            return false;
        }

        snapshotDiff = null;
        return true;
    }

    private string CreateMismatchDirectoryName()
        => CreateSnapshotDirectoryName(true);

    private string CreateSnapshotDirectoryName(bool mismatch = false)
    {
        var directoryName = GetDirectoryName(_fileName)!;

        return mismatch
            ? Combine(directoryName, "__snapshots__", "__mismatch__")
            : Combine(directoryName, "__snapshots__");
    }

    /// <summary>
    /// Ensure that the specified writer's underlying buffer ends with a newline.
    /// </summary>
    private static void EnsureEndOfBufferNewline(ArrayBufferWriter<byte> writer)
    {
        if (writer.WrittenSpan.Length > 0 && writer.WrittenSpan[^1] != (byte)'\n')
        {
            writer.Append("\n");
        }
    }

    private static void EnsureDirectoryExists(string file)
    {
        try
        {
            var directory = GetDirectoryName(file)!;

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch
        {
            // we ignore exception that could happen due to collisions
        }
    }

    private static void EnsureFileDoesNotExist(string file)
    {
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }

    private string CreateSnapshotFileName()
    {
        var fileName = GetFileNameWithoutExtension(_fileName);

        return string.IsNullOrEmpty(_postFix)
            ? string.Concat(fileName, _extension)
            : string.Concat(fileName, "_", _postFix, _extension);
    }

    private string CreateMarkdownSnapshotFileName()
    {
        var extension = _extension.Equals(".snap", StringComparison.Ordinal) ? ".md" : _extension;

        var fileName = GetFileNameWithoutExtension(_fileName);

        return string.IsNullOrEmpty(_postFix)
            ? string.Concat(fileName, extension)
            : string.Concat(fileName, "_", _postFix, extension);
    }

    private static string CreateFileName(StackFrame[] frames)
    {
        foreach (var stackFrame in frames)
        {
            var method = stackFrame.GetMethod();
            var fileName = stackFrame.GetFileName();

            if (method is not null
                && !string.IsNullOrEmpty(fileName)
                && s_testFramework.IsValidTestMethod(method))
            {
                return Combine(GetDirectoryName(fileName)!, method.ToName());
            }

            method = EvaluateAsynchronousMethodBase(method);

            if (method is not null
                && !string.IsNullOrEmpty(fileName)
                && s_testFramework.IsValidTestMethod(method))
            {
                return Combine(GetDirectoryName(fileName)!, method.ToName());
            }
        }

        throw new Exception(
            "The snapshot full name could not be evaluated. "
            + "This error can occur, if you use the snapshot match "
            + "within an async test helper child method. To solve this issue, "
            + "use the Snapshot.FullName directly in the unit test to "
            + "get the snapshot name, then reach this name to your "
            + "Snapshot.Match method.");
    }

    private static string CreateMarkdownTitle(StackFrame[] frames)
    {
        foreach (var stackFrame in frames)
        {
            var method = stackFrame.GetMethod();
            var fileName = stackFrame.GetFileName();

            if (method is not null
                && !string.IsNullOrEmpty(fileName)
                && s_testFramework.IsValidTestMethod(method))
            {
                return method.Name;
            }

            method = EvaluateAsynchronousMethodBase(method);

            if (method is not null
                && !string.IsNullOrEmpty(fileName)
                && s_testFramework.IsValidTestMethod(method))
            {
                return method.Name;
            }
        }

        throw new Exception(
            "The snapshot full name could not be evaluated. "
            + "This error can occur, if you use the snapshot match "
            + "within an async test helper child method. To solve this issue, "
            + "use the Snapshot.FullName directly in the unit test to "
            + "get the snapshot name, then reach this name to your "
            + "Snapshot.Match method.");
    }

    private static MethodInfo? EvaluateAsynchronousMethodBase(MemberInfo? method)
    {
        var methodDeclaringType = method?.DeclaringType;
        var classDeclaringType = methodDeclaringType?.DeclaringType;

        MethodInfo? actualMethodInfo = null;

        if (classDeclaringType != null)
        {
            var selectedMethodInfos =
                from methodInfo in classDeclaringType.GetMethods()
                let stateMachineAttribute = methodInfo
                    .GetCustomAttribute<AsyncStateMachineAttribute>()
                where stateMachineAttribute != null
                    && stateMachineAttribute.StateMachineType == methodDeclaringType
                select methodInfo;

            actualMethodInfo = selectedMethodInfos.SingleOrDefault();
        }

        return actualMethodInfo;
    }

    private static void CheckStrictMode()
    {
        var value = Environment.GetEnvironmentVariable("COOKIE_CRUMBLE_STRICT_MODE");

        if (string.Equals(value, "on", StringComparison.Ordinal)
            || (bool.TryParse(value, out var b) && b))
        {
            s_testFramework.ThrowTestException(
                "Strict mode is enabled and no snapshot has been found "
                + "for the current test. Create a new snapshot locally and "
                + "rerun your tests.");
        }
    }

    private readonly struct SnapshotSegment(string? name, object? value, ISnapshotValueFormatter formatter)
        : ISnapshotSegment
    {
        public string? Name { get; } = name;

        public object? Value { get; } = value;

        public ISnapshotValueFormatter Formatter { get; } = formatter;
    }
}
