using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections
{
    public class SelectionClosure
    {
        public SelectionClosure(Type clrType, string parameterName)
        {
            if (parameterName is null)
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

        public MemberInitExpression CreateMemberInit()
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

            if (TryGetSetType(sourceType, out Type? setType))
            {
                return ToSet(selection, setType);
            }

            return ToList(selection);
        }

        private Expression ToArray(Expression source)
        {
            return Expression.Call(
                typeof(Enumerable),
                  "ToArray",
                  new[] { ClrType },
                  source);
        }

        private Expression ToList(Expression source)
        {
            return Expression.Call(
                typeof(Enumerable),
                  "ToList",
                  new[] { ClrType },
                  source);
        }

        private Expression ToSet(
            Expression source,
            Type setType)
        {
            Type typedGeneric =
                setType.MakeGenericType(source.Type.GetGenericArguments()[0]);

            ConstructorInfo? ctor =
                typedGeneric.GetConstructor(new[] { source.Type });

            return Expression.New(ctor, source);
        }

        private bool TryGetSetType(
            Type type, [NotNullWhen(true)] out Type? setType)
        {
            if (type.IsGenericType)
            {
                Type typeDefinition = type.GetGenericTypeDefinition();
                if (typeDefinition == typeof(ISet<>)
                    || typeDefinition == typeof(HashSet<>))
                {
                    setType = typeof(HashSet<>);
                    return true;
                }
                else if (typeDefinition == typeof(SortedSet<>))
                {
                    setType = typeof(SortedSet<>);
                    return true;
                }
            }
            setType = default;
            return false;
        }
    }
}
