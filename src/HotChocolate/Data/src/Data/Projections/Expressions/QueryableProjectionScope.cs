using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions;

public readonly record struct AbstractTypeInfo(Type Type, IEnumerable<Expression> Initializers)
{
}

public class QueryableProjectionScope
    : ProjectionScope<Expression>
{
    private Dictionary<Type, IEnumerable<Expression>>? _abstractTypeInitializers;

    public QueryableProjectionScope(
        Type type,
        string parameterName)
    {
        Parameter = Expression.Parameter(type, parameterName);
        Instance.Push(Parameter);
        RuntimeType = type;
        Level = new Stack<Queue<Expression>>();
        Level.Push(new Queue<Expression>());
    }

    public Type RuntimeType { get; }

    ///<summary>
    /// Contains a queue for each level of the AST. The queues contain all operations of a level
    /// A new queue is needed when entering new <see cref="ObjectValueNode"/>
    ///</summary>
    public Stack<Queue<Expression>> Level { get; }

    public ParameterExpression Parameter { get; }

    public void AddAbstractType(Type type, IEnumerable<Expression> initializers)
    {
        _abstractTypeInitializers ??= new Dictionary<Type, IEnumerable<Expression>>();
        _abstractTypeInitializers[type] = initializers;
    }

    public IEnumerable<AbstractTypeInfo> GetAbstractTypes()
    {
        if (_abstractTypeInitializers is not null)
        {
            foreach (var elm in _abstractTypeInitializers)
            {
                yield return new(elm.Key, elm.Value);
            }
        }
    }

    public bool HasAbstractTypes() => _abstractTypeInitializers is not null;
}
