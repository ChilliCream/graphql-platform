using System.Buffers;
using System.Buffers.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    private const int MaxCapturedDepth = 8;

    private byte[] _buffer;
    private readonly int _initialSize;
    private int _start;

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

            // Reference-type property getters can execute user code. Captured closures use fields,
            // and value-type holders cover hoisted ExpressionParameter<T> values.
            switch (node.Member)
            {
                case FieldInfo field:
                    Append(Value);
                    AppendCapturedValue(field.GetValue(constant.Value), 0);
                    break;

                case PropertyInfo property
                    when property.GetIndexParameters().Length == 0 && constant.Value is ValueType:
                    Append(Value);
                    AppendCapturedValue(property.GetValue(constant.Value), 0);
                    break;

                default:
                    AppendMemberTypeFallback(node.Member, constant.Type);
                    break;
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
        if (depth > MaxCapturedDepth)
        {
            throw new NotSupportedException(
                "The captured value graph is too deep or cyclic and cannot be hashed into "
                + $"a DataLoader branch key (value type: {value?.GetType().FullName ?? "null"}).");
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
                break;

            case char c:
                AppendNumber(ValueKind.Char, c);
                break;

            case Enum e:
                AppendEnumValue(e);
                break;

            case sbyte i:
                AppendNumber(ValueKind.SByte, i);
                break;

            case byte i:
                AppendNumber(ValueKind.Byte, i);
                break;

            case short i:
                AppendNumber(ValueKind.Int16, i);
                break;

            case ushort i:
                AppendNumber(ValueKind.UInt16, i);
                break;

            case int i:
                AppendNumber(ValueKind.Int32, i);
                break;

            case uint i:
                AppendNumber(ValueKind.UInt32, i);
                break;

            case long i:
                AppendNumber(ValueKind.Int64, i);
                break;

            case ulong i:
                AppendNumber(ValueKind.UInt64, i);
                break;

            case float f:
                AppendNumber(ValueKind.Single, f);
                break;

            case double d:
                AppendNumber(ValueKind.Double, d);
                break;

            case decimal d:
                AppendNumber(ValueKind.Decimal, d);
                break;

            case DateTime d:
                AppendNumber(ValueKind.DateTime, d.Ticks);
                Append((byte)d.Kind);
                break;

            case DateTimeOffset d:
                AppendNumber(ValueKind.DateTimeOffset, d.Ticks);
                AppendRaw(d.Offset.Ticks);
                break;

            case TimeSpan t:
                AppendNumber(ValueKind.TimeSpan, t.Ticks);
                break;

            case DateOnly d:
                AppendNumber(ValueKind.DateOnly, d.DayNumber);
                break;

            case TimeOnly t:
                AppendNumber(ValueKind.TimeOnly, t.Ticks);
                break;

            case Guid g:
                Append(GuidTag);
                AppendRaw(g);
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
            // Sequences other than arrays and List<T> may be lazy and are never enumerated;
            // they contribute their type identity only.
            AppendTypeFallback(type);
            return;
        }

        if (type.IsValueType)
        {
            AppendStructValue(value, type, depth);
            return;
        }

        // Reference types are never evaluated beyond their type identity so that hashing
        // cannot execute user code.
        AppendTypeFallback(type);
    }

    private void AppendStringValue(string value)
    {
        Append(Str);
        Append(value.Length);
        Append(':');

        // The length prefix keeps string payloads from aliasing element boundaries. The
        // UTF-16 payload is hashed directly, which is lossless for any content, including
        // unpaired surrogates.
        Append(MemoryMarshal.AsBytes(value.AsSpan()));
    }

    private void AppendEnumValue(Enum value)
    {
        Append(EnumTag);
        AppendType(value.GetType());
        Append('|');
        Append(value.ToString("D"));
    }

    private void AppendNumber<T>(ValueKind kind, T value)
        where T : unmanaged
    {
        Append(Num);
        Append((byte)kind);
        AppendRaw(value);
    }

    private void AppendRaw<T>(T value)
        where T : unmanaged
    {
        Span<byte> bytes = stackalloc byte[16];
        MemoryMarshal.Write(bytes, in value);
        Append(bytes[..Unsafe.SizeOf<T>()]);
    }

    private void AppendCollection(System.Collections.IList items, Type type, int count, int depth)
    {
        Append(CollectionStart);
        AppendType(type);
        Append('|');
        Append(count);
        Append(':');

        for (var i = 0; i < count; i++)
        {
            AppendCapturedValue(items[i], depth + 1);
        }

        Append(CollectionEnd);
    }

    private void AppendStructValue(object value, Type type, int depth)
    {
        Append(Struct);
        AppendType(type);
        Append('|');

        // Struct field reads run no user code, so captured value types like record structs,
        // tuples, and composite keys can be folded safely.
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Array.Sort(fields, static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        foreach (var field in fields)
        {
            Append(field.Name);
            Append('=');
            AppendCapturedValue(field.GetValue(value), depth + 1);
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

    private static bool IsList(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

    private enum ValueKind : byte
    {
        SByte = 1,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
        Char,
        DateTime,
        DateTimeOffset,
        TimeSpan,
        DateOnly,
        TimeOnly
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
