using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections.Expressions
{
    public class QueryableProjectionScope
        : ProjectionScope<Expression>
    {
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
    }
}
