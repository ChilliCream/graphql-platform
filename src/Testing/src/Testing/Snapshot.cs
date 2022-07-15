using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Xunit;
using static System.Collections.Immutable.ImmutableStack;
using static System.IO.Path;

namespace Testing;

public sealed class Snapshot
{
    private static readonly object _sync = new();
    private static readonly UTF8Encoding _encoding = new();
    private static ImmutableStack<ISnapshotValueSerializer> _serializers =
        CreateRange<ISnapshotValueSerializer>(new[] { new GraphQLSnapshotValueSerializer() });
    private static readonly JsonSnapshotValueSerializer _defaultSerializer = new();

    private readonly List<SnapshotSegment> _segments = new();
    private readonly string _fileName;
    private readonly string? _postFix;
    private readonly string _extension;

    public Snapshot(string? postFix = null, string? extension = null)
    {
        _fileName = CreateFileName();
        _postFix = postFix;
        _extension = extension ?? ".snap";
    }

    public static void Match(
        object? value,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueSerializer? serializer = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value, serializer: serializer);
        snapshot.Match();
    }

    public static void Match(
        object? value1,
        object? value2,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueSerializer? serializer = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value1, serializer: serializer);
        snapshot.Add(value2, serializer: serializer);
        snapshot.Match();
    }

    public static void Match(
        object? value1,
        object? value2,
        object? value3,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueSerializer? serializer = null)
    {
        var snapshot = new Snapshot(postFix, extension);
        snapshot.Add(value1, serializer: serializer);
        snapshot.Add(value2, serializer: serializer);
        snapshot.Add(value3, serializer: serializer);
        snapshot.Match();
    }

    public static void Register(ISnapshotValueSerializer serializer)
    {
        lock (_sync)
        {
            _serializers = _serializers.Push(serializer);
        }
    }

    public void Add(object? value, string? name = null, ISnapshotValueSerializer? serializer = null)
    {
        serializer ??= FindSerializer(value);
        _segments.Add(new SnapshotSegment(name, value, serializer));
    }

    private static ISnapshotValueSerializer FindSerializer(object? value)
    {
        // we capture the current immutable serializer list
        var serializers = _serializers;

        // the we iterate over the captured stack.
        foreach (var serializer in serializers)
        {
            if (serializer.CanHandle(value))
            {
                return serializer;
            }
        }

        return _defaultSerializer;
    }

    public async ValueTask MatchAsync(CancellationToken cancellationToken = default)
    {
        var next = false;
        var writer = new ArrayBufferWriter<byte>();

        foreach (var segment in _segments)
        {
            if (next)
            {
                writer.AppendLine();
                writer.AppendSeparator();
                writer.AppendLine();
            }

            if (!string.IsNullOrEmpty(segment.Name))
            {
                writer.Append(segment.Name);
                writer.AppendLine();
                writer.AppendSeparator();
                writer.AppendLine();
            }

            segment.Serializer.Serialize(writer, segment.Value);
            next = true;
        }

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            await using var stream = File.Create(snapshotFile);
            await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());

            if (File.Exists(mismatchFile))
            {
                File.Delete(mismatchFile);
            }

            var before = await File.ReadAllTextAsync(snapshotFile, cancellationToken);
            var after = _encoding.GetString(writer.WrittenSpan);
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

                await using var stream = File.Create(mismatchFile);
                await stream.WriteAsync(writer.WrittenMemory, cancellationToken);
                throw new Xunit.Sdk.XunitException(output.ToString());
            }
        }
    }

    public void Match()
    {
        var next = false;
        var writer = new ArrayBufferWriter<byte>();

        foreach (var segment in _segments)
        {
            if (next)
            {
                writer.AppendLine();
                writer.AppendSeparator();
                writer.AppendLine();
            }

            if (!string.IsNullOrEmpty(segment.Name))
            {
                writer.Append(segment.Name);
                writer.AppendLine();
                writer.AppendSeparator();
                writer.AppendLine();
            }

            segment.Serializer.Serialize(writer, segment.Value);
            next = true;
        }

        var snapshotFile = Combine(CreateSnapshotDirectoryName(), CreateSnapshotFileName());

        if (!File.Exists(snapshotFile))
        {
            using var stream = File.Create(snapshotFile);
            stream.Write(writer.WrittenSpan);
        }
        else
        {
            var mismatchFile = Combine(CreateMismatchDirectoryName(), CreateSnapshotFileName());

            if (File.Exists(mismatchFile))
            {
                File.Delete(mismatchFile);
            }

            var before = File.ReadAllText(snapshotFile);
            var after = _encoding.GetString(writer.WrittenSpan);
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

                using var stream = File.Create(mismatchFile);
                stream.Write(writer.WrittenSpan);
                throw new Xunit.Sdk.XunitException(output.ToString());
            }
        }
    }

    private string CreateMismatchDirectoryName()
        => CreateSnapshotDirectoryName(true);

    private string CreateSnapshotDirectoryName(bool mismatch = false)
    {
        var directoryName = GetDirectoryName(_fileName)!;

        var directory = mismatch
            ? Combine(directoryName, "__snapshots__", "__MISMATCH__")
            : Combine(directoryName, "__snapshots__");

        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        catch
        {
            // we ignore exception that could happen due to collisions
        }

        return directory;
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
        public SnapshotSegment(string? name, object? value, ISnapshotValueSerializer serializer)
        {
            Name = name;
            Value = value;
            Serializer = serializer;
        }

        public string? Name { get; }

        public object? Value { get; }

        public ISnapshotValueSerializer Serializer { get; }
    }
}
