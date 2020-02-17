using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HotChocolate.Types.Selection
{
    public class SelectionClosure
    {
        public SelectionClosure(Type clrType, string parameterName)
        {
            if (parameterName == null)
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            ClrType = clrType ?? throw new ArgumentNullException(nameof(clrType));
            Parameter = Expression.Parameter(clrType, parameterName);
            Instance = new Stack<Expression>();
            Projections = new Dictionary<string, MemberAssignment>();
            Instance.Push(Parameter);
        }

        public Stack<Expression> Instance { get; }
        public Type ClrType { get; }
        public ParameterExpression Parameter { get; }

        public Dictionary<string, MemberAssignment> Projections { get; }

        public Expression CreateMemberInit()
        {
            NewExpression ctor = Expression.New(ClrType);
            return Expression.MemberInit(ctor, Projections.Values);
        }
        public Expression CreateMemberInitLambda()
        {
            return Expression.Lambda(CreateMemberInit(), Parameter);
        }

        public Expression CreateSelection(Expression source, Type sourceType)
        {
            MethodCallExpression selection = Expression.Call(
                typeof(Enumerable),
                  "Select",
                  new[] { ClrType, ClrType },
                  source,
                  CreateMemberInitLambda());

            if (sourceType.IsArray)
            {
                return ToArray(selection);
            }
            return selection;
        }

        private Expression ToArray(Expression source)
        {
            return Expression.Call(
                typeof(Enumerable),
                  "ToArray",
                  new[] { ClrType },
                  source);
        }
    }
}
