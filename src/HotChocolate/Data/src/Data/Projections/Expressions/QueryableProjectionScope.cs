using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions;

public class QueryableProjectionScope
    : ProjectionScope<Expression>
{
    private Dictionary<Type, IEnumerable<Expression>>? _abstractType;

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
        _abstractType ??= new Dictionary<Type, IEnumerable<Expression>>();
        _abstractType[type] = initializers;
    }

    public IEnumerable<KeyValuePair<Type, IEnumerable<Expression>>> GetAbstractTypes()
    {
        if (_abstractType is not null)
        {
            foreach (KeyValuePair<Type, IEnumerable<Expression>> elm in _abstractType)
            {
                yield return elm;
            }
        }
    }

    public bool HasAbstractTypes() => _abstractType is not null;
}
