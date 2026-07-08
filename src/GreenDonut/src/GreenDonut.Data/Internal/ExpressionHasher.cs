using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GreenDonut.Data.Internal;

internal sealed class ExpressionHasher : ExpressionVisitor
{
    private ReadOnlySpan<byte> Lambda => "Lambda|"u8;
    private ReadOnlySpan<byte> Binary => "Binary:"u8;
    private ReadOnlySpan<byte> Member => "Member:"u8;
    private ReadOnlySpan<byte> Parameter => "Parameter:"u8;
    private ReadOnlySpan<byte> Unary => "Unary:"u8;
    private ReadOnlySpan<byte> MethodCall => "MethodCall:"u8;
    private ReadOnlySpan<byte> New => "New:"u8;
    private ReadOnlySpan<byte> MemberInit => "MemberInit|"u8;
    private ReadOnlySpan<byte> Binding => "Binding:"u8;
    private ReadOnlySpan<byte> TypeBinary => "TypeBinary:"u8;
    private ReadOnlySpan<byte> Conditional => "Conditional|"u8;
    private ReadOnlySpan<byte> NewArray => "NewArray:"u8;
    private ReadOnlySpan<byte> Index => "Index:"u8;
    private ReadOnlySpan<byte> ListInit => "ListInit|"u8;
    private ReadOnlySpan<byte> Default => "Default:"u8;
    private ReadOnlySpan<byte> Invocation => "Invocation|"u8;
    private ReadOnlySpan<byte> Constant => "K:"u8;
    private ReadOnlySpan<byte> ConstantRoot => "CR:"u8;
    private ReadOnlySpan<byte> Value => "V:"u8;
    private ReadOnlySpan<byte> Null => "Z:"u8;
    private ReadOnlySpan<byte> Str => "S:"u8;
    private ReadOnlySpan<byte> Bool => "B:"u8;
    private ReadOnlySpan<byte> Num => "N:"u8;
    private ReadOnlySpan<byte> EnumTag => "E:"u8;
    private ReadOnlySpan<byte> Temporal => "T:"u8;
    private ReadOnlySpan<byte> GuidTag => "G:"u8;
    private ReadOnlySpan<byte> CollectionStart => "C[:"u8;
    private ReadOnlySpan<byte> CollectionEnd => "C]:"u8;
    private ReadOnlySpan<byte> Struct => "W:"u8;
    private ReadOnlySpan<byte> TypeFallback => "TF:"u8;
    private ReadOnlySpan<byte> Truncated => "TRUNC:"u8;
    private ReadOnlySpan<byte> ErrorTag => "ERR:"u8;

    private const int MaxCapturedNodes = 10_000;
    private const int MaxCapturedBytes = 64 * 1024;
    private const int MaxCapturedDepth = 3;
    private const int MaxCapturedStringChars = 2048;
    private const byte CharDiscriminator = (byte)TypeCode.Char;
    private const byte OtherFormattableDiscriminator = 103;

    private byte[] _buffer;
    private readonly int _initialSize;
    private int _start;
    private int _capturedNodeCount;
    private int _capturedByteCount;

    public ExpressionHasher()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(1024);
        _initialSize = _buffer.Length;
    }

    internal int BufferSize => _buffer.Length;
    internal int InitialBufferSize => _initialSize;

    public ExpressionHasher Add(Expression expression)
    {
        Visit(expression);
        Append(';');
        return this;
    }

    public ExpressionHasher Add<T>(SortDefinition<T> sortDefinition)
    {
        foreach (var operation in sortDefinition.Operations)
        {
            Visit(operation.KeySelector);
            Append('-', '>');
            Append(operation.Ascending ? (byte)1 : (byte)0);
            Append(';');
        }
        return this;
    }

    public ExpressionHasher Add(char c)
    {
        Append(c, ';');
        return this;
    }

    public ExpressionHasher Add(ReadOnlySpan<byte> span)
    {
        Append(span);
        Append(';');
        return this;
    }

    public ExpressionHasher Add(ReadOnlySpan<char> span)
    {
        Append(span);
        Append(';');
        return this;
    }

    public string Compute()
    {
        var hashBytes = MD5.HashData(_buffer.AsSpan()[.._start]);

#if NET9_0_OR_GREATER
        var hashString = Convert.ToHexStringLower(hashBytes);
#else
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
#endif

        _buffer.AsSpan()[.._start].Clear();

        if (_buffer.Length > _initialSize)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(_initialSize);
        }

        _start = 0;
        _capturedNodeCount = 0;
        _capturedByteCount = 0;
        return hashString;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        Append(Lambda);
        foreach (var parameter in node.Parameters)
        {
            Visit(parameter);
        }
        Visit(node.Body);
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Append(Binary);
        Append((int)node.NodeType);
        Append('|');
        Visit(node.Left);
        Visit(node.Right);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Append(Member);
        Append(node.Member);
        Append('|');

        if (node.Expression is ConstantExpression constant)
        {
            Append(ConstantRoot);
            AppendType(constant.Type);
            Append('|');

            object? value;
            bool evaluated;

            // Reference-type property getters can execute user code. Captured closures use fields,
            // and value-type holders cover hoisted ExpressionParameter<T> values.
            try
            {
                switch (node.Member)
                {
                    case FieldInfo field:
                        value = field.GetValue(constant.Value);
                        evaluated = true;
                        break;

                    case PropertyInfo property
                        when property.GetIndexParameters().Length == 0 && constant.Value is ValueType:
                        value = property.GetValue(constant.Value);
                        evaluated = true;
                        break;

                    default:
                        value = null;
                        evaluated = false;
                        break;
                }
            }
            catch
            {
                value = null;
                evaluated = false;
            }

            if (evaluated)
            {
                Append(Value);
                AppendCapturedValue(value, 0);
            }
            else
            {
                AppendMemberTypeFallback(node.Member, constant.Type);
            }

            return node;
        }

        Visit(node.Expression);
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        Append(Constant);
        AppendType(node.Type);
        Append('|');
        AppendCapturedValue(node.Value, 0);
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Append(Parameter);
        Append(node.Name ?? string.Empty);
        Append('|');
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        Append(Unary);
        Append((int)node.NodeType);
        Append('|');
        Visit(node.Operand);
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Append(MethodCall);
        Append(node.Method);
        Append('|');

        if (node.Method.IsGenericMethod)
        {
            foreach (var typeArgument in node.Method.GetGenericArguments())
            {
                AppendType(typeArgument);
                Append('|');
            }
        }

        Visit(node.Object);
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        Append(New);
        Append(node.Constructor);
        Append('|');
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
        return node;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        Append(MemberInit);
        Visit(node.NewExpression);
        foreach (var binding in node.Bindings)
        {
            VisitMemberBindingInternal(binding);
        }
        return node;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        Append(TypeBinary);
        Append((int)node.NodeType);
        Append('|');
        AppendType(node.TypeOperand);
        Append('|');
        Visit(node.Expression);
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Append(Conditional);
        Visit(node.Test);
        Visit(node.IfTrue);
        Visit(node.IfFalse);
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        Append(NewArray);
        Append((int)node.NodeType);
        Append('|');
        AppendType(node.Type);
        Append('|');
        foreach (var expression in node.Expressions)
        {
            Visit(expression);
        }
        return node;
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        Append(Index);
        Append(node.Indexer);
        Append('|');
        Visit(node.Object);
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
        return node;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        Append(ListInit);
        Visit(node.NewExpression);
        foreach (var initializer in node.Initializers)
        {
            Append(initializer.AddMethod);
            Append('|');
            foreach (var argument in initializer.Arguments)
            {
                Visit(argument);
            }
        }
        return node;
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        Append(Default);
        AppendType(node.Type);
        Append('|');
        return node;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        Append(Invocation);
        Visit(node.Expression);
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
        }
        return node;
    }

    private void VisitMemberBindingInternal(MemberBinding binding)
    {
        Append(Binding);
        Append((int)binding.BindingType);
        Append('|');
        Append(binding.Member);
        Append('|');

        switch (binding)
        {
            case MemberAssignment assignment:
                Visit(assignment.Expression);
                break;

            case MemberMemberBinding memberBinding:
                foreach (var subBinding in memberBinding.Bindings)
                {
                    VisitMemberBindingInternal(subBinding);
                }
                break;

            case MemberListBinding listBinding:
                foreach (var initializer in listBinding.Initializers)
                {
                    foreach (var argument in initializer.Arguments)
                    {
                        Visit(argument);
                    }
                }
                break;
        }
    }

    private void AppendCapturedValue(object? value, int depth)
    {
        if (++_capturedNodeCount > MaxCapturedNodes
            || _capturedByteCount > MaxCapturedBytes
            || depth > MaxCapturedDepth)
        {
            AppendTruncated(value);
            return;
        }

        switch (value)
        {
            case null:
                Append(Null);
                break;

            case string s:
                AppendStringValue(s);
                break;

            case bool b:
                Append(Bool);
                Append(b ? (byte)1 : (byte)0);
                AddCapturedBytes(1);
                break;

            case char c:
                AppendFormatted(Num, CharDiscriminator, (int)c, default);
                break;

            case Enum e:
                AppendEnumValue(e);
                break;

            case sbyte i:
                AppendFormatted(Num, (byte)TypeCode.SByte, i, default);
                break;

            case byte i:
                AppendFormatted(Num, (byte)TypeCode.Byte, i, default);
                break;

            case short i:
                AppendFormatted(Num, (byte)TypeCode.Int16, i, default);
                break;

            case ushort i:
                AppendFormatted(Num, (byte)TypeCode.UInt16, i, default);
                break;

            case int i:
                AppendFormatted(Num, (byte)TypeCode.Int32, i, default);
                break;

            case uint i:
                AppendFormatted(Num, (byte)TypeCode.UInt32, i, default);
                break;

            case long i:
                AppendFormatted(Num, (byte)TypeCode.Int64, i, default);
                break;

            case ulong i:
                AppendFormatted(Num, (byte)TypeCode.UInt64, i, default);
                break;

            case float f:
                AppendFormatted(Num, (byte)TypeCode.Single, f, default);
                break;

            case double d:
                AppendFormatted(Num, (byte)TypeCode.Double, d, default);
                break;

            case decimal d:
                AppendFormatted(Num, (byte)TypeCode.Decimal, d, default);
                break;

            case DateTime d:
                AppendFormatted(Temporal, (byte)TypeCode.DateTime, d, "O");
                break;

            case DateTimeOffset d:
                AppendFormatted(Temporal, (byte)'O', d, "O");
                break;

            case TimeSpan t:
                AppendFormatted(Temporal, (byte)'S', t, "c");
                break;

            case DateOnly d:
                AppendFormatted(Temporal, (byte)'D', d, "O");
                break;

            case TimeOnly t:
                AppendFormatted(Temporal, (byte)'Y', t, "O");
                break;

            case Guid g:
                AppendFormatted(GuidTag, (byte)'G', g, "D");
                break;

            default:
                AppendComplexValue(value, depth);
                break;
        }
    }

    private void AppendComplexValue(object value, int depth)
    {
        var type = value.GetType();

        if (type.IsArray)
        {
            if (value is Array { Rank: 1 } array && array.GetLowerBound(0) == 0)
            {
                AppendCollection(array, type, array.Length, depth);
            }
            else
            {
                AppendTypeFallback(type);
            }
            return;
        }

        if (IsList(type))
        {
            var list = (System.Collections.IList)value;
            AppendCollection(list, type, list.Count, depth);
            return;
        }

        if (value is System.Collections.IEnumerable)
        {
            // Other sequences may be lazy and can hit a database or throw when enumerated.
            AppendTypeFallback(type);
            return;
        }

        if (type.IsValueType)
        {
            if (value is IFormattable formattable)
            {
                AppendOtherFormattableValue(formattable, type);
            }
            else
            {
                AppendStructValue(value, type, depth);
            }
            return;
        }

        AppendTypeFallback(type);
    }

    private void AppendStringValue(string value)
    {
        Append(Str);
        Append(value.Length);
        Append(':');

        // The length prefix keeps string payloads from aliasing element boundaries. Hashing
        // the UTF-16 payload directly is lossless for any content, including unpaired
        // surrogates, and cannot fail.
        var chars = value.Length <= MaxCapturedStringChars
            ? value.AsSpan()
            : value.AsSpan(0, MaxCapturedStringChars);

        Append(MemoryMarshal.AsBytes(chars));
        AddCapturedBytes(chars.Length * 2);

        if (value.Length > MaxCapturedStringChars)
        {
            Append(Truncated);
        }
    }

    private void AppendEnumValue(Enum value)
    {
        var enumType = value.GetType();
        Append(EnumTag);
        AppendType(enumType);
        Append('|');

        try
        {
            var formatted = value.ToString("D");
            Append(formatted);
            AddCapturedBytes(formatted.Length);
        }
        catch
        {
            AppendError(enumType);
        }
    }

    private void AppendFormatted<T>(ReadOnlySpan<byte> tag, byte discriminator, T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
        Append(tag);
        Append(discriminator);
        Append(':');

        try
        {
            Span<byte> utf8 = stackalloc byte[128];
            if (value.TryFormat(utf8, out var written, format, CultureInfo.InvariantCulture))
            {
                Append(utf8[..written]);
                AddCapturedBytes(written);
                return;
            }
        }
        catch
        {
            // Fall through to the error marker.
        }

        AppendError(typeof(T));
    }

    private void AppendCollection(System.Collections.IList items, Type type, int count, int depth)
    {
        Append(CollectionStart);
        AppendType(type);
        Append('|');
        Append(count);
        Append(':');

        var position = _start;
        try
        {
            for (var i = 0; i < count; i++)
            {
                if (_capturedNodeCount > MaxCapturedNodes || _capturedByteCount > MaxCapturedBytes)
                {
                    // A single marker with the remaining count keeps the work bounded.
                    Append(Truncated);
                    Append(count - i);
                    Append('|');
                    break;
                }

                AppendCapturedValue(items[i], depth + 1);
            }
        }
        catch
        {
            _start = position;
            AppendError(type);
            return;
        }

        Append(CollectionEnd);
    }

    private void AppendStructValue(object value, Type type, int depth)
    {
        Append(Struct);
        AppendType(type);
        Append('|');

        var position = _start;

        // Struct field reads run no user code, so captured value types like record structs,
        // tuples, and composite keys can be folded safely.
        try
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Array.Sort(fields, static (a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var field in fields)
            {
                if (_capturedNodeCount > MaxCapturedNodes || _capturedByteCount > MaxCapturedBytes)
                {
                    Append(Truncated);
                    break;
                }

                Append(field.Name);
                Append('=');
                AppendCapturedValue(field.GetValue(value), depth + 1);
            }
        }
        catch
        {
            _start = position;
            AppendError(type);
            return;
        }

        Append('|');
    }

    private void AppendOtherFormattableValue(IFormattable value, Type type)
    {
        Append(Num);
        Append(OtherFormattableDiscriminator);
        Append(':');
        AppendType(type);
        Append('|');

        try
        {
            var formatted = value.ToString(null, CultureInfo.InvariantCulture);
            if (formatted is null)
            {
                Append(Null);
                return;
            }

            // Length-prefixed like any string so user-defined text cannot alias element boundaries.
            AppendStringValue(formatted);
        }
        catch
        {
            AppendError(type);
        }
    }

    private void AppendTruncated(object? value)
    {
        Append(Truncated);
        AddCapturedBytes(32);

        if (value is null)
        {
            Append(Null);
            return;
        }

        var type = value.GetType();
        AppendType(type);

        if (TryGetMaterializedCollectionCount(value, type, out var count))
        {
            Append('|');
            Append(count);
        }

        Append('|');
    }

    private void AppendMemberTypeFallback(MemberInfo member, Type holderType)
    {
        Append(TypeFallback);
        AppendType(holderType);
        Append('|');

        switch (member)
        {
            case FieldInfo field:
                AppendType(field.FieldType);
                break;

            case PropertyInfo property:
                AppendType(property.PropertyType);
                break;

            default:
                AppendType(member.DeclaringType ?? holderType);
                break;
        }

        Append('|');
    }

    private void AppendTypeFallback(Type type)
    {
        Append(TypeFallback);
        AppendType(type);
        Append('|');
    }

    private void AppendError(Type type)
    {
        Append(ErrorTag);
        AppendType(type);
        Append('|');
    }

    private static bool IsList(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

    private static bool TryGetMaterializedCollectionCount(object value, Type type, out int count)
    {
        if (value is Array { Rank: 1 } array && array.GetLowerBound(0) == 0)
        {
            count = array.Length;
            return true;
        }

        if (IsList(type) && value is System.Collections.ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        count = 0;
        return false;
    }

    private void AddCapturedBytes(int count)
    {
        if (count > 0)
        {
            _capturedByteCount += count;
        }
    }

    private void Append(int i)
    {
        int written;

        var span = _buffer.AsSpan()[_start..];
        while (!Utf8Formatter.TryFormat(i, span, out written))
        {
            span = ExpandRollingBufferCapacity(_start);
        }

        _start += written;
    }

    private void Append(ConstructorInfo? constructorInfo)
    {
        if (constructorInfo is null)
        {
            return;
        }

        var type = constructorInfo.ReflectedType?.FullName
            ?? constructorInfo.ReflectedType?.Name
            ?? constructorInfo.DeclaringType?.FullName
            ?? constructorInfo.DeclaringType?.Name
            ?? "global";

        Append(type);
        Append('.');
        Append("$ctor");
    }

    private void Append(MemberInfo? member)
    {
        if (member is null)
        {
            return;
        }

        var type = member.ReflectedType?.FullName
            ?? member.ReflectedType?.Name
            ?? member.DeclaringType?.FullName
            ?? member.DeclaringType?.Name
            ?? "global";

        Append(type);
        Append('.');
        Append(member.Name);
    }

    private void AppendType(Type type)
        => Append(type.FullName ?? type.Name);

    private void Append(char s)
    {
        if (_start == _buffer.Length)
        {
            ExpandBufferCapacity();
        }

        _buffer[_start++] = (byte)s;
    }

    private void Append(char a, char b)
    {
        if (_start + 1 >= _buffer.Length)
        {
            ExpandBufferCapacity();
        }

        _buffer[_start++] = (byte)a;
        _buffer[_start++] = (byte)b;
    }

    private void Append(string s)
    {
        var span = _buffer.AsSpan()[_start..];
        var chars = s.AsSpan();
        int written;

        while (!Encoding.UTF8.TryGetBytes(chars, span, out written))
        {
            span = ExpandRollingBufferCapacity(_start);
        }

        _start += written;
    }

    private void Append(ReadOnlySpan<char> s)
    {
        var span = _buffer.AsSpan()[_start..];
        int written;

        while (!Encoding.UTF8.TryGetBytes(s, span, out written))
        {
            span = ExpandRollingBufferCapacity(_start);
        }

        _start += written;
    }

    private void Append(byte b)
    {
        if (_start == _buffer.Length)
        {
            ExpandBufferCapacity();
        }

        _buffer[_start++] = b;
    }

    private void Append(ReadOnlySpan<byte> span)
    {
        var bufferSpan = _buffer.AsSpan()[_start..];

        while (!span.TryCopyTo(bufferSpan))
        {
            bufferSpan = ExpandRollingBufferCapacity(_start);
        }

        _start += span.Length;
    }

    private void ExpandBufferCapacity()
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
        _buffer.AsSpan().CopyTo(newBuffer);
        ReturnCleared(_buffer);
        _buffer = newBuffer;
    }

    private Span<byte> ExpandRollingBufferCapacity(int bufferIndex)
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
        _buffer.AsSpan()[..bufferIndex].CopyTo(newBuffer);
        ReturnCleared(_buffer);
        _buffer = newBuffer;
        return _buffer.AsSpan()[bufferIndex..];
    }

    private static void ReturnCleared(byte[] buffer)
    {
        // The buffer holds captured predicate values, so scrub it before handing it back
        // to the shared pool, mirroring the clear in Compute.
        buffer.AsSpan().Clear();
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
