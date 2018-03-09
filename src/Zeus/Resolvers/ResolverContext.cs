using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public class ResolverContext
        : IResolverContext
    {
        private readonly IServiceProvider _services;
        private readonly OperationContext _operationContext;
        private readonly Action<IBatchedQuery> _registerQuery;
        private readonly SelectionContext _selectionContext;
        private readonly Func<string, object> _getVariableValue;

        private ResolverContext(
            IServiceProvider services,
            OperationContext operationContext,
            Func<string, object> getVariableValue,
            Action<IBatchedQuery> registerQuery)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _operationContext = operationContext ?? throw new ArgumentNullException(nameof(operationContext));
            _getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
            _registerQuery = registerQuery ?? throw new ArgumentNullException(nameof(registerQuery));
            Path = ImmutableStack<object>.Empty;
        }

        private ResolverContext(
            IServiceProvider services,
            OperationContext operationContext,
            SelectionContext selectionContext,
            IImmutableStack<object> path,
            Func<string, object> getVariableValue,
            Action<IBatchedQuery> registerQuery)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _operationContext = operationContext ?? throw new ArgumentNullException(nameof(operationContext));
            _selectionContext = selectionContext ?? throw new ArgumentNullException(nameof(selectionContext));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            _getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
            _registerQuery = registerQuery ?? throw new ArgumentNullException(nameof(registerQuery));
        }

        public ISchema Schema => _operationContext.Schema;

        public ObjectTypeDefinition TypeDefinition => _selectionContext?.TypeDefinition;

        public FieldDefinition FieldDefinition => _selectionContext?.FieldDefinition;

        public QueryDocument QueryDocument => _operationContext.QueryDocument;

        public OperationDefinition OperationDefinition => _operationContext.Operation;

        public Field Field => _selectionContext?.Field;

        public IImmutableStack<object> Path { get; }

        public T Argument<T>(string name)
        {
            if (Field == null)
            {
                throw new InvalidOperationException("The current context has no selection context and thus does not provide any arguments.");
            }

            if (!Field.Arguments.ContainsKey(name)
                && !FieldDefinition.Arguments.ContainsKey(name))
            {
                throw new InvalidOperationException("The specified argument is not defined.");
            }

            if (Field.Arguments.TryGetValue(name, out Argument argument))
            {
                if (argument.Value is Variable v)
                {
                    return (T)_getVariableValue(v.Name);
                }
                return ValueConverter.Convert<T>(argument.Value);
            }

            IValue defaultValue = FieldDefinition.Arguments[name].DefaultValue;
            return ValueConverter.Convert<T>(defaultValue);
        }

        public T Parent<T>()
        {
            if (Path.Any())
            {
                return (T)Path.Peek();
            }

            throw new InvalidOperationException("The current context has no selection context and thus does not provide a parent.");
        }

        public void RegisterQuery(IBatchedQuery query)
        {
            _registerQuery(query);
        }

        public T Service<T>()
        {
            return (T)_services.GetService(typeof(T));
        }

        public IResolverContext Create(
            SelectionContext selectionContext,
            object result)
        {
            if (selectionContext == null)
            {
                throw new ArgumentNullException(nameof(selectionContext));
            }

            return new ResolverContext(_services, _operationContext, 
                selectionContext, Path.Push(result), _getVariableValue, 
                _registerQuery);
        }

        public static ResolverContext Create(
            IServiceProvider services,
            OperationContext operationContext,
            Func<string, object> getVariableValue,
            Action<IBatchedQuery> registerQuery)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (operationContext == null)
            {
                throw new ArgumentNullException(nameof(operationContext));
            }

            if (getVariableValue == null)
            {
                throw new ArgumentNullException(nameof(getVariableValue));
            }

            if (registerQuery == null)
            {
                throw new ArgumentNullException(nameof(registerQuery));
            }

            return new ResolverContext(services, operationContext,
                getVariableValue, registerQuery);
        }
    }
}