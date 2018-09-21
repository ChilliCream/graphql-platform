using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal class DirectiveTypeDescriptor
        : IDirectiveTypeDescriptor
        , IDescriptionFactory<DirectiveTypeDescription>
    {
        private readonly List<DirectiveArgumentDescriptor> _arguments =
            new List<DirectiveArgumentDescriptor>();

        private readonly Dictionary<MiddlewareKind, Func<string, IDirectiveMiddleware>> _middlewares
            = new Dictionary<MiddlewareKind, Func<string, IDirectiveMiddleware>>();

        protected DirectiveTypeDescription DirectiveDescription { get; } =
            new DirectiveTypeDescription();

        public DirectiveTypeDescription CreateDescription()
        {
            if (DirectiveDescription.Name == null)
            {
                throw new InvalidOperationException(
                    "A directive must have a name.");
            }

            CompleteArguments();
            CompleteMiddlewares();

            return DirectiveDescription;
        }

        protected virtual void CompleteArguments()
        {
            DirectiveDescription.Arguments.Clear();

            foreach (DirectiveArgumentDescriptor descriptor in _arguments)
            {
                DirectiveDescription.Arguments.Add(
                    descriptor.CreateDescription());
            }
        }

        private void CompleteMiddlewares()
        {
            DirectiveDescription.Middlewares.Clear();

            foreach (Func<string, IDirectiveMiddleware> middlewareFactory in
                _middlewares.Values)
            {
                DirectiveDescription.Middlewares.Add(
                    middlewareFactory(DirectiveDescription.Name));
            }
        }

        protected void SyntaxNode(DirectiveDefinitionNode syntaxNode)
        {
            DirectiveDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The directive name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL directive name.",
                    nameof(name));
            }

            DirectiveDescription.Name = name;
        }

        protected void Description(string description)
        {
            DirectiveDescription.Description = description;
        }

        protected DirectiveArgumentDescriptor Argument(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The directive argument name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsArgumentNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid " +
                    "GraphQL directive argument name.",
                    nameof(name));
            }

            var descriptor = new DirectiveArgumentDescriptor(name);
            _arguments.Add(descriptor);
            return descriptor;
        }

        protected DirectiveArgumentDescriptor Argument(
            DirectiveArgumentDescriptor descriptor)
        {
            _arguments.Add(descriptor);
            return descriptor;
        }

        protected void Location(DirectiveLocation location)
        {
            DirectiveDescription.Locations.Add(location);
        }

        protected void Resolver(OnInvokeResolverAsync resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _middlewares[MiddlewareKind.OnInvoke] = directiveName =>
                new DirectiveResolverMiddleware(
                    directiveName,
                    resolver);
        }

        protected void OnBeforeInvokeResolver(
            OnBeforeInvokeResolverAsync onBeforeInvoke)
        {
            if (onBeforeInvoke == null)
            {
                throw new ArgumentNullException(nameof(onBeforeInvoke));
            }

            _middlewares[MiddlewareKind.OnAfterInvoke] = directiveName =>
                new DirectiveOnBeforeInvokeMiddleware(
                    directiveName,
                    onBeforeInvoke);
        }

        protected void OnInvokeResolver(
            OnInvokeResolverAsync onInvoke)
        {
            if (onInvoke == null)
            {
                throw new ArgumentNullException(nameof(onInvoke));
            }

            _middlewares[MiddlewareKind.OnAfterInvoke] = directiveName =>
                new DirectiveResolverMiddleware(
                    directiveName,
                    onInvoke);
        }

        protected void OnAfterInvokeResolver(
            OnAfterInvokeResolverAsync onAfterInvoke)
        {
            if (onAfterInvoke == null)
            {
                throw new ArgumentNullException(nameof(onAfterInvoke));
            }

            _middlewares[MiddlewareKind.OnAfterInvoke] = directiveName =>
                new DirectiveOnAfterInvokeMiddleware(
                    directiveName,
                    onAfterInvoke);
        }

        protected void OnBeforeInvokeResolver<T>(
            Expression<Func<T, object>> method)
        {
            BindMethodAsMiddleware(method, MiddlewareKind.OnBeforeInvoke);
        }

        protected void OnInvokeResolver<T>(
            Expression<Func<T, object>> method)
        {
            BindMethodAsMiddleware(method, MiddlewareKind.OnInvoke);
        }

        protected void OnAfterInvokeResolver<T>(
            Expression<Func<T, object>> method)
        {
            BindMethodAsMiddleware(method, MiddlewareKind.OnAfterInvoke);
        }

        private void BindMethodAsMiddleware<T>(
            Expression<Func<T, object>> method,
            MiddlewareKind middlewareKind)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (method.ExtractMember() is MethodInfo m)
            {
                _middlewares[middlewareKind] = directiveName =>
                    new DirectiveMethodMiddleware(
                        directiveName,
                        middlewareKind,
                        typeof(T),
                        m);
            }

            throw new ArgumentException(
                "Only methods can be bound as directive middlewares.",
                nameof(method));
        }

        #region IDirectiveDescriptor

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.SyntaxNode(
            DirectiveDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IArgumentDescriptor IDirectiveTypeDescriptor.Argument(string name)
        {
            return Argument(name);
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Location(
            DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor
            .OnBeforeInvokeResolver(OnBeforeInvokeResolverAsync onBeforeInvoke)
        {
            OnBeforeInvokeResolver(onBeforeInvoke);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.OnInvokeResolver(
            OnInvokeResolverAsync onInvoke)
        {
            OnInvokeResolver(onInvoke);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.OnAfterInvokeResolver(
            OnAfterInvokeResolverAsync onAfterInvoke)
        {
            OnAfterInvokeResolver(onAfterInvoke);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor
            .OnBeforeInvokeResolver<T>(Expression<Func<T, object>> method)
        {
            OnBeforeInvokeResolver(method);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.OnInvokeResolver<T>(
            Expression<Func<T, object>> method)
        {
            OnInvokeResolver(method);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor
            .OnAfterInvokeResolver<T>(Expression<Func<T, object>> method)
        {
            OnAfterInvokeResolver(method);
            return this;
        }

        #endregion
    }

    internal class DirectiveTypeDescriptor<T>
        : DirectiveTypeDescriptor
        , IDirectiveTypeDescriptor<T>

    {
        public DirectiveTypeDescriptor()
        {
            DirectiveDescription.ClrType = typeof(T);
            DirectiveDescription.Name = typeof(T).GetGraphQLName();
        }

        protected override void CompleteArguments()
        {
            base.CompleteArguments();

            var descriptions =
                new Dictionary<string, DirectiveArgumentDescription>();
            var handledProperties = new List<PropertyInfo>();

            AddExplicitArguments(descriptions, handledProperties);

            if (DirectiveDescription.ArgumentBindingBehavior ==
                BindingBehavior.Implicit)
            {
                Dictionary<PropertyInfo, string> properties =
                    GetPossibleImplicitArguments(handledProperties);
                AddImplicitArguments(descriptions, properties);
            }

            DirectiveDescription.Arguments.Clear();
            DirectiveDescription.Arguments.AddRange(descriptions.Values);
        }

        private void AddExplicitArguments(
            Dictionary<string, DirectiveArgumentDescription> descriptors,
            List<PropertyInfo> handledProperties)
        {
            foreach (DirectiveArgumentDescription argumentDescription in
                DirectiveDescription.Arguments)
            {
                if (!argumentDescription.Ignored)
                {
                    descriptors[argumentDescription.Name] = argumentDescription;
                }

                if (argumentDescription.Property != null)
                {
                    handledProperties.Add(argumentDescription.Property);
                }
            }
        }

        private void AddImplicitArguments(
            Dictionary<string, DirectiveArgumentDescription> descriptors,
            Dictionary<PropertyInfo, string> properties)
        {
            foreach (KeyValuePair<PropertyInfo, string> property in properties)
            {
                if (!descriptors.ContainsKey(property.Value))
                {
                    Type returnType = property.Key.GetReturnType();
                    if (returnType != null)
                    {
                        var argDescriptor = new DirectiveArgumentDescriptor(
                            property.Value, property.Key);

                        descriptors[property.Value] = argDescriptor
                            .CreateDescription();
                    }
                }
            }
        }

        private Dictionary<PropertyInfo, string> GetPossibleImplicitArguments(
            List<PropertyInfo> handledProperties)
        {
            Dictionary<PropertyInfo, string> properties = GetProperties(
                DirectiveDescription.ClrType);

            foreach (PropertyInfo property in handledProperties)
            {
                properties.Remove(property);
            }

            return properties;
        }

        private static Dictionary<PropertyInfo, string> GetProperties(Type type)
        {
            var members = new Dictionary<PropertyInfo, string>();

            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public)
                .Where(t => t.DeclaringType != typeof(object)))
            {
                members[property] = property.GetGraphQLName();
            }

            return members;
        }

        protected void BindArguments(BindingBehavior bindingBehavior)
        {
            DirectiveDescription.ArgumentBindingBehavior = bindingBehavior;
        }

        protected DirectiveArgumentDescriptor Argument(
            Expression<Func<T, object>> property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.ExtractMember() is PropertyInfo p)
            {
                var descriptor = new DirectiveArgumentDescriptor(
                    p.GetGraphQLName(), p);
                return Argument(descriptor);
            }

            throw new ArgumentException(
                "Only properties are allowed in this expression.",
                nameof(property));
        }

        #region IDirectiveDescriptor<T>

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.SyntaxNode(
            DirectiveDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Name(
            string name)
        {
            Name(name);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Description(
          string description)
        {
            Description(description);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.BindArguments(
            BindingBehavior bindingBehavior)
        {
            BindArguments(bindingBehavior);
            return this;
        }

        IDirectiveArgumentDescriptor IDirectiveTypeDescriptor<T>.Argument(
            Expression<Func<T, object>> property)
        {
            return Argument(property);
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Location(
            DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnBeforeInvokeResolver(OnBeforeInvokeResolverAsync onBeforeInvoke)
        {
            OnBeforeInvokeResolver(onBeforeInvoke);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnBeforeInvokeResolver<TMiddleware>(
                Expression<Func<TMiddleware, object>> method)
        {
            OnBeforeInvokeResolver(method);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnInvokeResolver(OnInvokeResolverAsync onInvoke)
        {
            OnInvokeResolver(onInvoke);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnInvokeResolver<TMiddleware>(
                Expression<Func<TMiddleware, object>> method)
        {
            OnInvokeResolver(method);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnAfterInvokeResolver(OnAfterInvokeResolverAsync onAfterInvoke)
        {
            OnAfterInvokeResolver(onAfterInvoke);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>
            .OnAfterInvokeResolver<TMiddleware>(
                Expression<Func<TMiddleware, object>> method)
        {
            OnAfterInvokeResolver(method);
            return this;
        }

        #endregion
    }
}
