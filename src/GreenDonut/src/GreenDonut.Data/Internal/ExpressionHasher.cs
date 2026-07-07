using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace GreenDonut.Data.Internal;

internal sealed class ExpressionHasher : ExpressionVisitor
{
    private const int MaxCapturedValueDepth = 8;

    private ReadOnlySpan<byte> Lambda => "Lambda|"u8;
    private ReadOnlySpan<byte> Binary => "Binary:"u8;
    private ReadOnlySpan<byte> Member => "Member:"u8;
    private ReadOnlySpan<byte> Parameter => "Parameter:"u8;
    private ReadOnlySpan<byte> Unary => "Unary:"u8;
    private ReadOnlySpan<byte> MethodCall => "MethodCall:"u8;
    private ReadOnlySpan<byte> New => "New:"u8;
    private ReadOnlySpan<byte> MemberInit => "MemberInit|"u8;
    private ReadOnlySpan<byte> Binding => "Binding:"u8;
    private ReadOnlySpan<byte> Value => "Value:"u8;
    private ReadOnlySpan<byte> Null => "null"u8;

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

        // A member access rooted directly at a constant is a hoisted/captured
        // value - e.g. HotChocolate's ExpressionParameter<T>.p that filtering
        // uses to parameterize filter values, or a compiler-generated closure
        // field. The base traversal visits the constant, which contributes no
        // bytes, so two predicates that differ only by their captured value
        // (e.g. `status eq A` vs `status eq B`, or `status in [..]` with
        // different lists) produced identical hashes and collided onto the same
        // DataLoader branch key. Evaluate the captured value and fold it into
        // the hash instead of hashing only the member name.
        if (node.Expression is ConstantExpression constant)
        {
            Append(Value);
            AppendCapturedValue(TryGetMemberValue(node.Member, constant.Value), 0);
            Append('|');
            return node;
        }

        Visit(node.Expression);
        return node;
    }

    private static object? TryGetMemberValue(MemberInfo member, object? instance)
    {
        try
        {
            return member switch
            {
                FieldInfo field => field.GetValue(instance),

                // Only read properties off value-type holders (record structs
                // like ExpressionParameter<T>). Closures expose captured values
                // as fields, so this never invokes an arbitrary reference-type
                // getter - which could be expensive or have side effects - just
                // to compute a branch key.
                PropertyInfo { CanRead: true } property
                    when property.GetIndexParameters().Length == 0
                        && instance is ValueType
                    => property.GetValue(instance),

                _ => null
            };
        }
        catch
        {
            // A branch key must never fail because a captured member's accessor
            // throws, has side effects, or the instance is null. Fall back to
            // treating the value as absent instead of propagating the exception.
            return null;
        }
    }

    private void AppendCapturedValue(object? value, int depth)
    {
        switch (value)
        {
            case null:
                Append(Null);
                return;

            case string s:
                AppendDelimited(s);
                return;

            case bool b:
                Append(b ? (byte)1 : (byte)0);
                return;

            case char c:
                Append((int)c);
                return;

            case Enum e:
                AppendDelimited(e.GetType().FullName ?? e.GetType().Name);
                Append('.');
                AppendDelimited(e.ToString());
                return;

            case IFormattable formattable:
                // numbers, decimal, Guid, DateTime, DateTimeOffset, TimeSpan, ...
                AppendDelimited(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
        }

        if (depth >= MaxCapturedValueDepth)
        {
            AppendDelimited(value.GetType().FullName ?? value.GetType().Name);
            return;
        }

        // Never enumerate a queryable (or other lazy provider) while hashing:
        // doing so could execute a database query or another side effect just to
        // compute a branch key. Fold in its type instead.
        if (value is IQueryable)
        {
            AppendDelimited(value.GetType().FullName ?? value.GetType().Name);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            Append('[');
            foreach (var item in enumerable)
            {
                AppendCapturedValue(item, depth + 1);
                Append(',');
            }
            Append(']');
            return;
        }

        // Complex holder (e.g. ExpressionParameter<T> or a closure display
        // class): fold in its instance fields so that captured values embedded
        // in the holder still differentiate the hash. Fields are ordered to keep
        // the hash stable across runtimes.
        var type = value.GetType();
        AppendDelimited(type.FullName ?? type.Name);
        Append('{');

        var fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Array.Sort(fields, static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        foreach (var field in fields)
        {
            AppendDelimited(field.Name);
            Append('=');
            AppendCapturedValue(TryGetMemberValue(field, value), depth + 1);
            Append(';');
        }

        Append('}');
    }

    private void AppendDelimited(string value)
    {
        // Length-prefix variable-length text so a separator embedded in a value
        // (e.g. a comma inside a filtered string) cannot produce the same byte
        // sequence as a different set of values - i.e. ["a,b"] must not hash the
        // same as ["a", "b"].
        Append(value.Length);
        Append(':');
        Append(value);
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
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private Span<byte> ExpandRollingBufferCapacity(int bufferIndex)
    {
        var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
        _buffer.AsSpan()[..bufferIndex].CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
        return _buffer.AsSpan()[bufferIndex..];
    }
}
