using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
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
            IDictionary<string, DirectiveArgumentDescription> descriptors,
            ICollection<PropertyInfo> handledProperties)
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
            IDictionary<string, DirectiveArgumentDescription> descriptors,
            IDictionary<PropertyInfo, string> properties)
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
            ICollection<PropertyInfo> handledProperties)
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

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Middleware(
            DirectiveMiddleware middleware)
        {
            Middleware(middleware);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Middleware<TM>(
            Expression<Func<TM, object>> method)
        {
            Middleware(method);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Middleware<TM>(
            Expression<Action<T>> method)
        {
            Middleware(method);
            return this;
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Repeatable()
        {
            DirectiveDescription.IsRepeatable = true;
            return this;
        }

        #endregion
    }
}
