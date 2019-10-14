using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Sorting
{
    public class SortQueryableClosure
    {
        public SortQueryableClosure(Type type, string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Instance = new Stack<Expression>();

            Instance.Push(Parameter);
        }
        public ParameterExpression Parameter;
        public Stack<Expression> Instance { get; }

        public SortOperationInvocation CreateSortOperation(SortOperationKind kind)
        {
            return new SortOperationInvocation(kind, Parameter, Instance.Peek());
        }

        public void EnqueueProperty(PropertyInfo property)
        {
            var nestedProperty = Expression.Property(
                Instance.Peek(), property
            );
            Instance.Push(nestedProperty);
        }
    }
}
