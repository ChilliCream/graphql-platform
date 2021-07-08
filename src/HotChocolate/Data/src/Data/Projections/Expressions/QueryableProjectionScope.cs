using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionScope
        : ProjectionScope<Expression>
    {
        private Dictionary<Type, Queue<MemberAssignment>>? _abstractType;

        public QueryableProjectionScope(
            Type type,
            string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Instance.Push(Parameter);
            RuntimeType = type;
            Level = new Stack<Queue<MemberAssignment>>();
            Level.Push(new Queue<MemberAssignment>());
        }

        public Type RuntimeType { get; }

        ///<summary>
        /// Contains a queue for each level of the AST. The queues contain all operations of a level
        /// A new queue is needed when entering new <see cref="ObjectValueNode"/>
        ///</summary>
        public Stack<Queue<MemberAssignment>> Level { get; }

        public ParameterExpression Parameter { get; }

        public void AddAbstractType(Type type, Queue<MemberAssignment> memberAssignments)
        {
            _abstractType ??= new Dictionary<Type, Queue<MemberAssignment>>();
            _abstractType[type] = memberAssignments;
        }

        public IEnumerable<KeyValuePair<Type, Queue<MemberAssignment>>> GetAbstractTypes()
        {
            if (_abstractType is not null)
            {
                foreach (KeyValuePair<Type, Queue<MemberAssignment>> elm in _abstractType)
                {
                    yield return elm;
                }
            }
        }

        public bool HasAbstractTypes() => _abstractType is not null;
    }
}
