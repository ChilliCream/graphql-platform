using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using CookieCrumble.Formatters;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Xunit;
using static System.Collections.Immutable.ImmutableStack;
using static System.IO.Path;

namespace CookieCrumble;

public class Snapshot
{
    private static readonly object _sync = new();
    private static readonly UTF8Encoding _encoding = new();
    private static ImmutableStack<ISnapshotValueFormatter> _formatters =
        CreateRange(new ISnapshotValueFormatter[]
        {
            new PlainTextSnapshotValueFormatter(),
            new GraphQLSnapshotValueFormatter(),
            new ExecutionResultSnapshotValueFormatter(),
            new SchemaSnapshotValueFormatter(),
            new ExceptionSnapshotValueFormatter(),
            new SchemaErrorSnapshotValueFormatter(),
            new HttpResponseSnapshotValueFormatter(),
            new OperationResultSnapshotValueFormatter(),
#if NET7_0_OR_GREATER
            new QueryPlanSnapshotValueFormatter(),
#endif
        });
    private static readonly JsonSnapshotValueFormatter _defaultFormatter = new();

    private readonly List<SnapshotSegment> _segments = [];
    private readonly string _fileName;
    private string _extension;
    private string? _postFix;

    public Snapshot(string? postFix = null, string? extension = null)
    {
        _fileName = CreateFileName();
        _postFix = postFix;
        _extension = extension ?? ".snap";
    }

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

    public static void RegisterFormatter(
        ISnapshotValueFormatter formatter)
    {
        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        lock (_sync)
        {
            _formatters = _formatters.Push(formatter);
        }
    }

    public static void TryRegisterFormatter(
        ISnapshotValueFormatter formatter,
        bool typeCheck = true)
    {
        if (formatter is null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        lock (_sync)
        {
            if (typeCheck)
            {
                var type = formatter.GetType();
                if (_formatters.Any(t => t.GetType() == type))
                {
                    return;
                }
            }
            else
            {
                if (_formatters.Contains(formatter))
                {
                    return;
                }
            }

            _formatters = _formatters.Push(formatter);
        }
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

    public Snapshot SetExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            throw new ArgumentNullException(nameof(extension));
        }

        _extension = extension;
        return this;
    }

    public Snapshot SetPostFix(string postFix)
    {
        if (string.IsNullOrEmpty(postFix))
        {
            throw new ArgumentNullException(nameof(postFix));
        }

        _postFix = postFix;
        return this;
    }

    private static ISnapshotValueFormatter FindSerializer(object? value)
    {
        // we capture the current immutable serializer list
        var serializers = _formatters;

        // the we iterate over the captured stack.
        foreach (var serializer in serializers)
        {
            if (serializer.CanHandle(value))
            {
                return serializer;
            }
        }

        return _defaultFormatter;
    }

    public async ValueTask MatchAsync(CancellationToken cancellationToken = default)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            EnsureDirectoryExists(snapshotFile);
            await using var stream = File.Create(snapshotFile);
            await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);

            var before = await File.ReadAllTextAsync(snapshotFile, cancellationToken);
            var after = _encoding.GetString(writer.WrittenSpan);

            if (!MatchSnapshot(before, after, false, out var diff))
            {
                EnsureDirectoryExists(mismatchFile);
                await using var stream = File.Create(mismatchFile);
                await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
                throw new Xunit.Sdk.XunitException(diff);
            }
        }
    }

    public void Match()
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            EnsureDirectoryExists(snapshotFile);
            using var stream = File.Create(snapshotFile);
            stream.Write(writer.WrittenSpan);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());
            EnsureFileDoesNotExist(mismatchFile);
            var before = File.ReadAllText(snapshotFile);
            var after = _encoding.GetString(writer.WrittenSpan);

            if (!MatchSnapshot(before, after, false, out var diff))
            {
                EnsureDirectoryExists(mismatchFile);
                using var stream = File.Create(mismatchFile);
                stream.Write(writer.WrittenSpan);
                throw new Xunit.Sdk.XunitException(diff);
            }
        }
    }

    public void MatchInline(string expected)
    {
        var writer = new ArrayBufferWriter<byte>();
        WriteSegments(writer);

        var after = _encoding.GetString(writer.WrittenSpan);

        if (!MatchSnapshot(expected, after, true, out var diff))
        {
            throw new Xunit.Sdk.XunitException(diff);
        }
    }

    private void WriteSegments(IBufferWriter<byte> writer)
    {
        if (_segments.Count == 1)
        {
            var segment = _segments[0];
            segment.Formatter.Format(writer, segment.Value);
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

            segment.Formatter.Format(writer, segment.Value);

            writer.AppendLine();
            writer.AppendSeparator();
            writer.AppendLine();

            next = true;
        }
    }

    private static bool MatchSnapshot(
        string before,
        string after,
        bool inline,
        [NotNullWhen(false)] out string? snapshotDiff)
    {
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
            ? Combine(directoryName, "__snapshots__", "__MISMATCH__")
            : Combine(directoryName, "__snapshots__");
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

    private static string CreateFileName()
    {
        foreach (var stackFrame in new StackTrace(true).GetFrames())
        {
            var method = stackFrame?.GetMethod();
            var fileName = stackFrame?.GetFileName();

            if (method is not null &&
                !string.IsNullOrEmpty(fileName) &&
                IsXunitTestMethod(method))
            {
                return Combine(GetDirectoryName(fileName)!, method.ToName());
            }

            var asyncMethod = EvaluateAsynchronousMethodBase(method);

            if (asyncMethod is not null &&
                !string.IsNullOrEmpty(fileName) &&
                IsXunitTestMethod(asyncMethod))
            {
                return Combine(GetDirectoryName(fileName)!, asyncMethod.ToName());
            }
        }

        throw new Exception(
            "The snapshot full name could not be evaluated. " +
            "This error can occur, if you use the snapshot match " +
            "within a async test helper child method. To solve this issue, " +
            "use the Snapshot.FullName directly in the unit test to " +
            "get the snapshot name, then reach this name to your " +
            "Snapshot.Match method.");
    }

    private static MethodBase? EvaluateAsynchronousMethodBase(MemberInfo? method)
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
                where stateMachineAttribute != null &&
                    stateMachineAttribute.StateMachineType == methodDeclaringType
                select methodInfo;

            actualMethodInfo = selectedMethodInfos.SingleOrDefault();
        }

        return actualMethodInfo;
    }

    private static bool IsXunitTestMethod(MemberInfo? method)
    {
        var isFactTest = IsFactTestMethod(method);
        var isTheoryTest = IsTheoryTestMethod(method);

        return isFactTest || isTheoryTest;
    }

    private static bool IsFactTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(FactAttribute)).Any() ?? false;

    private static bool IsTheoryTestMethod(MemberInfo? method)
        => method?.GetCustomAttributes(typeof(TheoryAttribute)).Any() ?? false;

    private struct SnapshotSegment
    {
        public SnapshotSegment(string? name, object? value, ISnapshotValueFormatter formatter)
        {
            Name = name;
            Value = value;
            Formatter = formatter;
        }

        public string? Name { get; }

        public object? Value { get; }

        public ISnapshotValueFormatter Formatter { get; }
    }
}

public sealed class DisposableSnapshot(string? postFix = null, string? extension = null)
    : Snapshot(postFix, extension)
    , IDisposable
{
    public void Dispose() => Match();
}