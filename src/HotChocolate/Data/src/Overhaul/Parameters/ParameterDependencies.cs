using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.ExpressionNodes;

public class ParameterDependencies : IReadOnlyParameterDependencies
{
    IReadOnlySet<Identifier>? IReadOnlyParameterDependencies.Ids => _ids;

    private readonly HashSet<Identifier>? _ids;

    public ParameterDependencies(HashSet<Identifier>? ids)
    {
        _ids = ids;
    }
}

public interface IReadOnlyParameterDependencies
{
    IReadOnlySet<Identifier>? Ids { get; }
}

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

public readonly struct ParameterBoxesEnumerable
{
    public ParameterBoxesEnumerable(
        ReadOnlyStructuralDependencies dependencies,
        IParameterContext context)
    {
        _dependencies = dependencies;
        _context = context;
    }

    private readonly ReadOnlyStructuralDependencies _dependencies;
    private readonly IParameterContext _context;

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly IParameterContext _parameters;
        private readonly bool _iteratingAllParameters;
        private HashSet<Identifier>.Enumerator _idEnumerator;
        private Dictionary<Identifier, BoxExpression>.Enumerator _parameterEnumerator;

        public Enumerator(ParameterBoxesEnumerable enumerable)
        {
            _parameters = enumerable._context;
            _iteratingAllParameters = enumerable._dependencies.Unspecified;
            if (_iteratingAllParameters)
                _idEnumerator = ((HashSet<Identifier>) enumerable._dependencies.VariableIds!).GetEnumerator();
            else
                _parameterEnumerator = ((Dictionary<Identifier, BoxExpression>) enumerable._context.Expressions).GetEnumerator();
        }

        public bool MoveNext()
        {
            if (_iteratingAllParameters)
                return _parameterEnumerator.MoveNext();
            else
                return _idEnumerator.MoveNext();
        }

        public readonly (Identifier Id, BoxExpression Box) Current
        {
            get
            {
                if (_iteratingAllParameters)
                {
                    var (id, box) = _parameterEnumerator.Current;
                    return (id, box);
                }
                else
                {
                    var id = _idEnumerator.Current;
                    var box = _parameters.GetParameter(id);
                    return (id, box);
                }
            }
        }
    }
}
