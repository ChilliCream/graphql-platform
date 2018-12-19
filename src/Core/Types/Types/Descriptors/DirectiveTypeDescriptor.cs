using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

        private Func<string, IDirectiveMiddleware> _middlewareFactory;

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
            DirectiveDescription.Middleware =
                _middlewareFactory?.Invoke(DirectiveDescription.Name);
        }

        protected void SyntaxNode(DirectiveDefinitionNode syntaxNode)
        {
            DirectiveDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(NameString name)
        {
            DirectiveDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected void Description(string description)
        {
            DirectiveDescription.Description = description;
        }

        protected DirectiveArgumentDescriptor Argument(NameString name)
        {
            var descriptor = new DirectiveArgumentDescriptor(
                name.EnsureNotEmpty(nameof(name)));
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
            var values = Enum.GetValues(typeof(DirectiveLocation));
            foreach (DirectiveLocation value in values)
            {
                if (location.HasFlag(value))
                {
                    DirectiveDescription.Locations.Add(value);
                }
            }
        }

        protected void Middleware(DirectiveMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareFactory =
                name => new DirectiveDelegateMiddleware(name, middleware);
        }

        protected void Middleware<T>(
            Expression<Func<T, object>> method)
        {
            BindMethodAsMiddleware(method);
        }

        protected void Middleware<T>(
            Expression<Action<T>> method)
        {
            BindMethodAsMiddleware(method);
        }

        private void BindMethodAsMiddleware<T>(
            Expression<Func<T, object>> method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            BindMethodAsMiddleware(
                typeof(T),
                method.ExtractMember() as MethodInfo);
        }

        private void BindMethodAsMiddleware<T>(Expression<Action<T>> method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            BindMethodAsMiddleware(
                typeof(T),
                method.ExtractMember() as MethodInfo);
        }

        private void BindMethodAsMiddleware(Type type, MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(
                    nameof(method),
                    "Only methods can be bound as directive middlewares.");
            }

            _middlewareFactory = name =>
                new DirectiveMethodMiddleware(
                    name,
                    type,
                    method);
        }

        #region IDirectiveDescriptor

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.SyntaxNode(
            DirectiveDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Name(NameString name)
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

        IArgumentDescriptor IDirectiveTypeDescriptor.Argument(NameString name)
        {
            return Argument(name);
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Location(
            DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Middleware(
            DirectiveMiddleware middleware)
        {
            Middleware(middleware);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Middleware<T>(
            Expression<Func<T, object>> method)
        {
            Middleware(method);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Middleware<T>(
            Expression<Action<T>> method)
        {
            Middleware(method);
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
            DirectiveDescription.Description =
                typeof(T).GetGraphQLDescription();
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
            return ReflectionUtils
                .GetProperties(type)
                .ToDictionary(t => t.Value, t => t.Key);
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
            NameString name)
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

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor<T>.Middleware(
            DirectiveMiddleware middleware)
        {
            Middleware(middleware);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor<T>.Middleware<TM>(
            Expression<Func<TM, object>> method)
        {
            Middleware(method);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor<T>.Middleware<TM>(
            Expression<Action<T>> method)
        {
            Middleware(method);
            return this;
        }

        #endregion
    }
}
