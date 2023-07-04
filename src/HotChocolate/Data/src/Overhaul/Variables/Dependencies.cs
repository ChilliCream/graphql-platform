using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.ExpressionNodes;

public readonly struct ReadOnlyStructuralDependencies
{
    [MemberNotNullWhen(false, nameof(Unspecified))]
    public IReadOnlySet<Identifier>? VariableIds { get; init; }

    public bool Unspecified => VariableIds is null;

    public static ReadOnlyStructuralDependencies All => new() { VariableIds = null };

    private static readonly HashSet<Identifier> _none = new();
    public static ReadOnlyStructuralDependencies None => new() { VariableIds = _none };
}

public readonly struct StructuralDependencies
{
    [MemberNotNullWhen(false, nameof(Unspecified))]
    public HashSet<Identifier>? VariableIds { get; init; }

    public bool Unspecified => VariableIds is null;

    public static StructuralDependencies All => new() { VariableIds = null };
    public static StructuralDependencies None => new() { VariableIds = new() };
}

public readonly struct Dependencies
{
    public StructuralDependencies Structural { get; init; }
}

public readonly struct VariableExpressionsEnumerable
{
    public VariableExpressionsEnumerable(
        ReadOnlyStructuralDependencies dependencies,
        IVariableContext context)
    {
        _dependencies = dependencies;
        _context = context;
    }

    private readonly ReadOnlyStructuralDependencies _dependencies;
    private readonly IVariableContext _context;

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly IVariableContext _variables;
        private readonly bool _iteratingAll;
        private HashSet<Identifier>.Enumerator _idEnumerator;
        private Dictionary<Identifier, BoxExpression>.Enumerator _boxExpressionsEnumerator;

        public Enumerator(VariableExpressionsEnumerable enumerable)
        {
            _variables = enumerable._context;
            _iteratingAll = enumerable._dependencies.Unspecified;
            if (_iteratingAll)
                _idEnumerator = ((HashSet<Identifier>) enumerable._dependencies.VariableIds!).GetEnumerator();
            else
                _boxExpressionsEnumerator = ((Dictionary<Identifier, BoxExpression>) enumerable._context.Expressions).GetEnumerator();
        }

        public bool MoveNext()
        {
            if (_iteratingAll)
                return _boxExpressionsEnumerator.MoveNext();
            else
                return _idEnumerator.MoveNext();
        }

        public readonly (Identifier Id, BoxExpression Box) Current
        {
            get
            {
                if (_iteratingAll)
                {
                    var (id, box) = _boxExpressionsEnumerator.Current;
                    return (id, box);
                }
                else
                {
                    var id = _idEnumerator.Current;
                    var box = _variables.GetParameter(id);
                    return (id, box);
                }
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class DependencyAttribute : Attribute
{
    public bool Structural { get; set; }
    public bool Expression { get; set; }
}
