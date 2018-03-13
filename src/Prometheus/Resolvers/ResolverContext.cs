using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Prometheus.Abstractions;
using Prometheus.Execution;

namespace Prometheus.Resolvers
{
    public class ResolverContext
        : IResolverContext
    {
        private readonly IServiceProvider _services;
        private readonly OperationContext _operationContext;
        private readonly Action<IBatchedQuery> _registerQuery;
        private readonly SelectionContext _selectionContext;
        private readonly IVariableCollection _variables;

        private ResolverContext(
            IServiceProvider services,
            OperationContext operationContext,
            IVariableCollection variables,
            Action<IBatchedQuery> registerQuery)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _operationContext = operationContext ?? throw new ArgumentNullException(nameof(operationContext));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _registerQuery = registerQuery ?? throw new ArgumentNullException(nameof(registerQuery));
            Path = ImmutableStack<object>.Empty;
        }

        private ResolverContext(
            IServiceProvider services,
            OperationContext operationContext,
            SelectionContext selectionContext,
            IImmutableStack<object> path,
            IVariableCollection variables,
            Action<IBatchedQuery> registerQuery)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _operationContext = operationContext ?? throw new ArgumentNullException(nameof(operationContext));
            _selectionContext = selectionContext ?? throw new ArgumentNullException(nameof(selectionContext));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _registerQuery = registerQuery ?? throw new ArgumentNullException(nameof(registerQuery));
        }

        public ISchema Schema => _operationContext.Schema;

        public ObjectTypeDefinition TypeDefinition => _selectionContext?.TypeDefinition;

        public FieldDefinition FieldDefinition => _selectionContext?.FieldDefinition;

        public IQueryDocument QueryDocument => _operationContext.QueryDocument;

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

            IValue argumentValue = GetArgumentValue(name);
            if (argumentValue is Variable v)
            {
                return _variables.GetVariable<T>(v.Name);
            }
            return ValueConverter.Convert<T>(argumentValue);
        }

        private IValue GetArgumentValue(string name)
        {
            if (Field.Arguments.TryGetValue(name, out Argument argument))
            {
                return argument.Value;
            }
            return FieldDefinition.Arguments[name].DefaultValue;
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
                selectionContext, Path.Push(result), _variables,
                _registerQuery);
        }

        public static ResolverContext Create(
            IServiceProvider services,
            OperationContext operationContext,
            IVariableCollection variables,
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

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            if (registerQuery == null)
            {
                throw new ArgumentNullException(nameof(registerQuery));
            }

            return new ResolverContext(services, operationContext,
                variables, registerQuery);
        }
    }
}