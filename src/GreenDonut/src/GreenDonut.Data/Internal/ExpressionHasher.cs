using System.Buffers;
using System.Buffers.Text;
using System.Linq.Expressions;
using System.Reflection;
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

    private byte[] _buffer;
    private readonly int _initialSize;
    private int _start;

    public ExpressionHasher()
    {
        _buffer = ArrayPool<byte>.Shared.Rent(1024);
        _initialSize = _buffer.Length;
    }

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
        Append(c);
        Append(';');
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
        var hashBytes = MD5.HashData(_buffer.AsSpan().Slice(0, _start));

#if NET9_0_OR_GREATER
        var hashString = Convert.ToHexStringLower(hashBytes);
#else
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
#endif

        _buffer.AsSpan().Slice(0, _start).Clear();

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
        Visit(node.Expression);
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

        while (!Utf8Formatter.TryFormat(i, _buffer.AsSpan().Slice(_start), out written))
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().Slice(0, _start).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
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
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        _buffer[_start++] = (byte)s;
    }

    private void Append(char a, char b)
    {
        if (_start + 1 == _buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        _buffer[_start++] = (byte)a;
        _buffer[_start++] = (byte)b;
    }

    private void Append(string s)
    {
        var span = _buffer.AsSpan().Slice(_start);
        var chars = s.AsSpan();
        int written;

        while (!Encoding.UTF8.TryGetBytes(chars, span, out written))
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().Slice(0, _start).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
            span = _buffer.AsSpan().Slice(_start);
        }

        _start += written;
    }

    private void Append(ReadOnlySpan<char> s)
    {
        var span = _buffer.AsSpan().Slice(_start);
        int written;

        while (!Encoding.UTF8.TryGetBytes(s, span, out written))
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().Slice(0, _start).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
            span = _buffer.AsSpan().Slice(_start);
        }

        _start += written;
    }

    private void Append(byte b)
    {
        if (_start == _buffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
        }

        _buffer[_start++] = b;
    }

    private void Append(ReadOnlySpan<byte> span)
    {
        var bufferSpan = _buffer.AsSpan().Slice(_start);

        while (!span.TryCopyTo(bufferSpan))
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
            _buffer.AsSpan().Slice(0, _start).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuffer;
            bufferSpan = _buffer.AsSpan().Slice(_start);
        }

        _start += span.Length;
    }
}
