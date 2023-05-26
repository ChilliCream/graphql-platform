using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions;

public readonly record struct AbstractTypeInfo(Type Type, IEnumerable<Expression> Initializers)
{
}

public class QueryableProjectionScope
    : ProjectionScope<Expression>
{
    private List<AbstractTypeInfo>? _abstractTypeInitializers;

    public QueryableProjectionScope(
        Type type,
        string parameterName)
    {
        Parameter = Expression.Parameter(type, parameterName);
        Instance.Push(Parameter);
        RuntimeType = type;
        Level = new Stack<LevelOperations>();
        Level.Push(new LevelOperations());
    }

    public Type RuntimeType { get; }

    ///<summary>
    /// Contains a queue for each level of the AST. The queues contain all operations of a level
    /// A new queue is needed when entering new <see cref="ObjectValueNode"/>
    ///</summary>
    public Stack<LevelOperations> Level { get; }

    public ParameterExpression Parameter { get; }

    public void AddAbstractType(Type type, IEnumerable<Expression> initializers)
    {
        _abstractTypeInitializers ??= new();
        _abstractTypeInitializers.Add(new(type, initializers));
    }

    public IEnumerable<AbstractTypeInfo> GetAbstractTypes()
    {
        if (_abstractTypeInitializers is null)
        {
            return Enumerable.Empty<AbstractTypeInfo>();
        }
        return _abstractTypeInitializers;
    }

    public bool HasAbstractTypes() => _abstractTypeInitializers is not null;
}
