using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class DirectiveTypeDescriptor
        : IDirectiveTypeDescriptor
        , IDescriptionFactory<DirectiveTypeDescription>
    {
        private readonly List<ArgumentDescriptor> _arguments =
            new List<ArgumentDescriptor>();

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

            return DirectiveDescription;
        }

        protected virtual void CompleteArguments()
        {
            foreach (DirectiveArgumentDescriptor descriptor in _arguments)
            {
                DirectiveDescription.Arguments.Add(
                    descriptor.CreateDescription());
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

        protected ArgumentDescriptor Argument(string name)
        {
            ArgumentDescriptor descriptor = new ArgumentDescriptor(name);
            _arguments.Add(descriptor);
            return descriptor;
        }

        protected void Location(DirectiveLocation location)
        {
            DirectiveDescription.Locations.Add(location);
        }

        #region IDirectiveDescriptor

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.SyntaxNode(DirectiveDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IArgumentDescriptor IDirectiveTypeDescriptor.Argument(string name)
        {
            return Argument(name);
        }

        IDirectiveTypeDescriptor IDirectiveTypeDescriptor.Location(DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        #endregion
    }

    internal class DirectiveDescriptor<T>
        : DirectiveTypeDescriptor
        , IDirectiveTypeDescriptor<T>

    {
        protected override void CompleteArguments()
        {
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

        IArgumentDescriptor IDirectiveTypeDescriptor<T>.Argument(
            Expression<Func<T, object>> property)
        {
            throw new NotImplementedException();
        }

        IDirectiveTypeDescriptor<T> IDirectiveTypeDescriptor<T>.Location(
            DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        #endregion
    }
}
