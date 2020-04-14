using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types.Sorting
{
    public class SortQueryableClosure
    {
        public SortQueryableClosure(Type type, string parameterName)
        {
            Parameter = Expression.Parameter(type, parameterName);
            Instance = new Stack<Expression>();
            Property = new Stack<PropertyInfo>();

            Instance.Push(Parameter);
        }

        public ParameterExpression Parameter { get; }

        public Stack<Expression> Instance { get; }

        public Stack<PropertyInfo> Property { get; }

        public void EnqueueProperty(PropertyInfo property)
        {
            Property.Push(property);
            Instance.Push(Expression.Property(Instance.Peek(), property));
        }

        public Expression Pop()
        {
            Property.Pop();
            return Instance.Pop();
        }
    }
}
