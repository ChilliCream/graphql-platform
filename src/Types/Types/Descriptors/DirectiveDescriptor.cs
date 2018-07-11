using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class DirectiveDescriptor
        : IDirectiveDescriptor
        , IDescriptionFactory<DirectiveDescription>
    {
        private readonly List<ArgumentDescriptor> _arguments =
            new List<ArgumentDescriptor>();

        protected DirectiveDescription DirectiveDescription { get; } =
            new DirectiveDescription();

        public DirectiveDescription CreateDescription()
        {
            if(DirectiveDescription.Name == null)
            {
                throw new InvalidOperationException(
                    "A directive must have a name.");
            }

            foreach (ArgumentDescriptor descriptor in _arguments)
            {
                DirectiveDescription.Arguments.Add(descriptor.CreateDescription());
            }

            return DirectiveDescription;
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

        IDirectiveDescriptor IDirectiveDescriptor.SyntaxNode(DirectiveDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IDirectiveDescriptor IDirectiveDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IDirectiveDescriptor IDirectiveDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IArgumentDescriptor IDirectiveDescriptor.Argument(string name)
        {
            return Argument(name);
        }

        IDirectiveDescriptor IDirectiveDescriptor.Location(DirectiveLocation location)
        {
            Location(location);
            return this;
        }

        #endregion
    }
}
